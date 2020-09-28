using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Kogane.DebugMenu
{
	/// <summary>
	/// ゲームオブジェクトのリストを作成するクラス
	/// </summary>
	public sealed class GameObjectListCreator : ListCreatorBase<CommandData>
	{
		//==============================================================================
		// クラス
		//==============================================================================
		/// <summary>
		/// ゲームオブジェクトの情報を管理するクラス
		/// </summary>
		private sealed class GameObjectData
		{
			//==========================================================================
			// 変数(readonly)
			//==========================================================================
			private readonly int    m_index;
			private readonly string m_name;
			private readonly int    m_parentCount;

			//==========================================================================
			// 変数
			//==========================================================================
			public GameObject m_gameObject; // 削除後に null を代入するので readonly ではない

			//==========================================================================
			// プロパティ
			//==========================================================================
			public string Text
			{
				get
				{
					var line        = ( m_index + 1 ).ToString( "0000" );
					var indent      = Repeat( "  ", m_parentCount );
					var isDestroyed = m_gameObject == null;
					var isActive    = !isDestroyed && m_gameObject.activeInHierarchy && m_gameObject.activeSelf;
					var colorTag    = isDestroyed ? "red" : isActive ? "while" : "silver";

					return $"<color={colorTag}>{line}  {indent}{m_name}</color>";
				}
			}

			//==========================================================================
			// 関数
			//==========================================================================
			/// <summary>
			/// コンストラクタ
			/// </summary>
			public GameObjectData( int index, GameObject gameObject )
			{
				m_index       = index;
				m_gameObject  = gameObject;
				m_name        = gameObject.name;
				m_parentCount = GetAllParent( m_gameObject ).Length;
			}

			/// <summary>
			/// アクティブかどうかを切り替えます
			/// </summary>
			public void ToggleActive()
			{
				if ( m_gameObject == null ) return;
				m_gameObject.SetActive( !m_gameObject.activeSelf );
			}

			/// <summary>
			/// ゲームオブジェクトを削除します
			/// </summary>
			public void Destroy()
			{
				GameObject.Destroy( m_gameObject );
				m_gameObject = null;
			}
		}

		//==============================================================================
		// 変数
		//==============================================================================
		private CommandData[] m_list;

		//==============================================================================
		// プロパティ
		//==============================================================================
		public override int Count => m_list.Length;

		//==============================================================================
		// 関数
		//==============================================================================
		/// <summary>
		/// リストの表示に使用するデータを作成します
		/// </summary>
		protected override void DoCreate( ListCreateData data )
		{
			/*
			 * http://baba-s.hatenablog.com/entry/2019/03/13/214323
			 * ゲームオブジェクトを順番に並べるために
			 * Resources.FindObjectsOfTypeAll ではなく
			 * GameObject.FindObjectsOfType を使用しています
			 */
			m_list = GameObject
					.FindObjectsOfType<GameObject>()
					.Where( x => x.transform.parent == null )
					.SelectMany( x => x.GetComponentsInChildren<Transform>( true ) )
					.Select( x => x.gameObject )
					.Select( ( val, index ) => new GameObjectData( index, val ) )
					.Where( x => data.IsMatch( x.Text ) )
					.Select
					(
						x =>
						{
							var elemData = new CommandData
							(
								() => x.Text,
								new ActionData
								(
									"詳細", () =>
									{
										if ( x.m_gameObject == null ) return;

										var components = x.m_gameObject
												.GetComponents<MonoBehaviour>()
												.Select( behaviour => JsonUtility.ToJson( behaviour, true ) )
											;

										var infoText = string.Join( "\n", components );

										OpenAdd( DMType.TEXT_TAB_6, new SimpleInfoCreator( infoText ) );
									}
								),
								new ActionData
								(
									"削除", () =>
									{
										x.Destroy();
										Refresh();
									}
								),
								new ActionData
								(
									"アクティブ\n切り替え", () =>
									{
										x.ToggleActive();
										Refresh();
									}
								)
							)
							{
								IsLeft = true,
							};
							return elemData;
						}
					)
					.ToArray()
				;

			if ( data.IsReverse )
			{
				Array.Reverse( m_list );
			}
		}

		/// <summary>
		/// 指定されたインデックスの要素の表示に使用するデータを返します
		/// </summary>
		protected override CommandData DoGetElemData( int index )
		{
			return m_list.ElementAtOrDefault( index );
		}

		/// <summary>
		/// すべての親オブジェクトを返します
		/// </summary>
		private static GameObject[] GetAllParent( GameObject self )
		{
			var result = new List<GameObject>();
			for ( var parent = self.transform.parent; parent != null; parent = parent.parent )
			{
				result.Add( parent.gameObject );
			}

			return result.ToArray();
		}

		/// <summary>
		/// 指定された文字列を指定された回数分繰り返し連結した文字列を生成して返します
		/// </summary>
		private static string Repeat( string self, int repeatCount )
		{
			var builder = new StringBuilder();
			for ( int i = 0; i < repeatCount; i++ )
			{
				builder.Append( self );
			}

			return builder.ToString();
		}
	}
}