using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Nez.AI.GOAP
{
	public class GoapAIDebuggerWindow : EditorWindow {
		private struct TitleData {
			public string text;
			public Rect   rect;
		}

		private GUIStyle _titleStyle;
		private GUIStyle _nodeStyle;
		private GUIStyle _stateStyle;
		private GUIStyle _goalStyle;
		private GUIStyle _activeGoalStyle;
		private GUIStyle _planStyle;
		private GUIStyle _activePlanStyle;
		private GUIStyle _failedPlanStyle;
		private GUIStyle _activeFailedPlanStyle;
		private GUIStyle _warningNodeStyle;

		private AIScenarioAgent            _agent;
		private List< GoapAIDebuggerNode > _nodes;
		private List< GoapAIDebuggerNode > _genericNodes;
		private List< GoapAIDebuggerNode > _goalNodes;
		private Vector2                    _planNodesPosition;
		private Stack< Action >            _currentPlan;
		private List< TitleData >          _titles;
		private GoapAIDebuggerNode         _blackboardNode;

		private Vector2 _offset;
		private Vector2 _drag;
		private Vector2 _totalDrag;

		private string _titleColor = "#61AFEF";
		private string _nameColor  = "#e5c07b";
		private string _valueColor = "#51a9b0";
		private string _trueColor  = "#98C35F";
		private string _falseColor = "#Dd6870";

		#region Initialize Window

		[MenuItem ( "Window/Nez-Goap AI Debugger" )]
		private static void ShowWindow () {
			GoapAIDebuggerWindow window =
				( GoapAIDebuggerWindow ) EditorWindow.GetWindow ( typeof ( GoapAIDebuggerWindow ), false,
					"AI Debugger" );
			window.autoRepaintOnSceneChange = true;
		}
		
		public static void OpenWindow ( AIScenarioAgent agent ) {
			GoapAIDebuggerWindow window =
				( GoapAIDebuggerWindow ) EditorWindow.GetWindow ( typeof ( GoapAIDebuggerWindow ), false,
					"AI Debugger" );
			window.autoRepaintOnSceneChange = true;
			window.SetupAgent ( agent );
		}

		private void SetupAgent ( AIScenarioAgent agent ) {
			
			if ( agent == null ) {
				_nodes.Clear ();
				_genericNodes.Clear ();
				_goalNodes.Clear ();
				_titles.Clear ();

				if ( _agent != null ) {
					_agent._planner.PlanUpdated -= OnPlanUpdated;
					_agent                      =  null;
				}
			}
			else if ( !System.Object.ReferenceEquals ( agent, _agent ) ) {
				_agent = agent;

				_nodes.Clear ();
				_goalNodes.Clear ();
				_titles.Clear ();

				CreateTitle ( 0.0f, 0.0f, string.Format ( "{0}: Actions and Goals", Selection.activeGameObject.name ) );

				float actionsHeight = 0.0f;
				RebuildActionNodes ( new Vector2 ( _totalDrag.x, _totalDrag.y + 35.0f ), out actionsHeight );
				actionsHeight += 35.0f;

				float goalsHeight = 0.0f;
				RebuildGoalNodes ( new Vector2 ( _totalDrag.x, _totalDrag.y + actionsHeight ), out goalsHeight );

				CreateTitle ( 0.0f, actionsHeight + goalsHeight + 15.0f, string.Format ( "{0}: Current Plan",  Selection.activeGameObject.name ) );
				_planNodesPosition = new Vector2 ( 0.0f, actionsHeight + goalsHeight + 45.0f );

				RebuildBlackboardNode ( _totalDrag + _planNodesPosition );

				_agent._planner.PlanUpdated += OnPlanUpdated;
			}
			
		}

		#endregion

		#region Unity Callbacks

		private void OnEnable () {
			_titleStyle                  = new GUIStyle ();
			_titleStyle.fontSize         = 22;
			_titleStyle.normal.textColor = Color.gray;

			_nodeStyle             = CreateNodeStyle ( "node0.png" );
			_stateStyle            = CreateNodeStyle ( "node0.png" );
			_goalStyle             = CreateNodeStyle ( "node1.png" );
			_activeGoalStyle       = CreateNodeStyle ( "node1 on.png" );
			_planStyle             = CreateNodeStyle ( "node0.png" );
			_activePlanStyle       = CreateNodeStyle ( "node0 on.png" );
			_failedPlanStyle       = CreateNodeStyle ( "node6.png" );
			_activeFailedPlanStyle = CreateNodeStyle ( "node6 on.png" );
			_warningNodeStyle      = CreateNodeStyle ( "node6.png" );

			_nodes        = new List< GoapAIDebuggerNode > ();
			_genericNodes = new List< GoapAIDebuggerNode > ();
			_goalNodes    = new List< GoapAIDebuggerNode > ();
			_titles       = new List< TitleData > ();
		}

		private void OnGUI () {
			
			if ( Selection.activeGameObject != null ) {
				var p = Selection.activeGameObject.GetComponent< AIScenarioAgentComponent > ();
				if ( p == null ) {
					_nodes.Clear ();
					_genericNodes.Clear ();
					_goalNodes.Clear ();
					_titles.Clear ();

					if ( _agent != null ) {
						_agent._planner.PlanUpdated -= OnPlanUpdated;
						_agent                      =  null;
					}
				}
				else if ( !System.Object.ReferenceEquals ( p.agent, _agent ) ) {
					_agent = p.agent;

					_nodes.Clear ();
					_goalNodes.Clear ();
					_titles.Clear ();

					CreateTitle ( 0.0f, 0.0f, string.Format ( "{0}: Actions and Goals", Selection.activeGameObject.name ) );

					float actionsHeight = 0.0f;
					RebuildActionNodes ( new Vector2 ( _totalDrag.x, _totalDrag.y + 35.0f ), out actionsHeight );
					actionsHeight += 35.0f;

					float goalsHeight = 0.0f;
					RebuildGoalNodes ( new Vector2 ( _totalDrag.x, _totalDrag.y + actionsHeight ), out goalsHeight );

					CreateTitle ( 0.0f, actionsHeight + goalsHeight + 15.0f, string.Format ( "{0}: Current Plan",  Selection.activeGameObject.name ) );
					_planNodesPosition = new Vector2 ( 0.0f, actionsHeight + goalsHeight + 45.0f );

					RebuildBlackboardNode ( _totalDrag + _planNodesPosition );

					_agent._planner.PlanUpdated += OnPlanUpdated;
				}
			}

			if ( _agent != null ) {
				DrawGrid ( 20, Color.gray, 0.05f );
				DrawGrid ( 100, Color.gray, 0.05f );

				if ( Event.current.type == EventType.Repaint ) {
					DrawTitles ( _titles );
					DrawLinks ( _nodes, true );
					DrawLinks ( _genericNodes, false );
					DrawCurrentStateLink ();
					DrawNodes ( _nodes );
					DrawNodes ( _genericNodes );
					Repaint ();
				}

				ProcessEvents ( Event.current );
			}
			else {
				if ( Event.current.type == EventType.Repaint ) {
					GUI.Label ( new Rect ( 10.0f, 10.0f, 200.0f, 50.0f ), "Object with AI Not Selected.", _titleStyle );
				}
			}
		}

		#endregion

		#region Event Handlers

		private void OnPlanUpdated ( Stack<Action> plan ) {
			float v = 0.0f;
			_currentPlan = plan;
			UpdatePlan ( _totalDrag + _planNodesPosition, out v );

			UpdateGoalNodes ();
			// UpdateBlackboardNode ();
			// if ( _blackboardNode != null ) {
			// 	var p = _totalDrag + _planNodesPosition;
			// 	p.y                           += v;
			// 	_blackboardNode.rect.position =  p;
			// }
		}

		#endregion

		#region Private Methods

		private GUIStyle CreateNodeStyle ( string aPic ) {
			var style = new GUIStyle ();
			style.normal.background =
				EditorGUIUtility.Load ( string.Concat ( "builtin skins/darkskin/images/", aPic ) ) as Texture2D;
			style.border           = new RectOffset ( 12, 12, 12, 12 );
			style.richText         = true;
			style.padding          = new RectOffset ( 12, 0, 10, 0 );
			style.normal.textColor = new Color ( 0.639f, 0.65f, 0.678f );
			return style;
		}

		private void RebuildActionNodes ( Vector2 aNodePosition, out float aMaxHeigth ) {
			
			Action action;
			var unusedActions = new List< KeyValuePair< Action, bool > > ();
			for ( int i = 0, n = _agent._planner._actions.Count; i < n; i++ ) {
				action = _agent._planner._actions[ i ];
				unusedActions.Add ( new KeyValuePair< Action, bool > ( action, false ) );
			}

			aMaxHeigth = 0.0f;
			float totalWidth  = 0.0f;
			float totalHeight = 0.0f;
			int   foundCount  = 0;
			bool  isDefaultState;

			GoapAIDebuggerNode actionNode;

			var toLinkNodes = new List< GoapAIDebuggerNode > ();

			for ( int j = 0, nj = _agent._planner._actions.Count; j < nj; j++ ) {
					
				toLinkNodes.Clear ();
				foundCount  = 0;
				totalWidth  = 0.0f;
				totalHeight = 0.0f;
					
				action = _agent._planner._actions[ j ];
				
				int id = unusedActions.FindIndex ( x => System.Object.ReferenceEquals ( x.Key, action ) );
				unusedActions[ id ] = new KeyValuePair< Action, bool > ( action, true );

				actionNode = CreateActionNode ( action, ref aNodePosition );
				actionNode.SetOutput ( actionNode.rect.width * 0.5f, actionNode.rect.height - 10.0f );
				totalWidth += actionNode.rect.width;
				totalHeight = ( actionNode.rect.height > totalHeight )
					? actionNode.rect.height
					: totalHeight;

				toLinkNodes.Add ( actionNode );
				foundCount++;
			}

			// for ( int i = 0, n = unusedActions.Count; i < n; i++ ) {
			// 	if ( !unusedActions[ i ].Value ) {
			// 		actionNode = CreateActionNode ( unusedActions[ i ].Key, ref aNodePosition );
			// 		actionNode.SetOutput ( actionNode.rect.width * 0.5f, actionNode.rect.height - 10.0f );
			// 		totalWidth  = actionNode.rect.width;
			// 		totalHeight = actionNode.rect.height;
			//
			// 		statePos  = new Vector2 ( aNodePosition.x - totalWidth, aNodePosition.y + totalHeight );
			// 		stateNode = CreateMissingStateNode ( unusedActions[ i ].Key.state, statePos );
			// 		stateNode.SetInput ( stateNode.rect.width * 0.5f, 10.0f );
			// 		actionNode.LinkTo ( stateNode, new Color ( 0.7f, 0.2f, 0.3f ) );
			//
			// 		totalHeight += stateNode.rect.height;
			// 		aMaxHeigth = ( totalHeight > aMaxHeigth )
			// 			? totalHeight
			// 			: aMaxHeigth;
			// 	}
			// }

			// Создаем оставшиеся задачи.
			totalHeight = 0.0f;
			// statePos    = aNodePosition;
			// for ( int i = 0, n = unusedStates.Count; i < n; i++ ) {
			// 	isDefaultState = System.Object.ReferenceEquals ( _agent.defaultState, unusedStates[ i ] );
			// 	stateNode      = CreateStateNode ( unusedStates[ i ], statePos, isDefaultState );
			// 	stateNode.SetInput ( stateNode.rect.width * 0.5f, 10.0f );
			// 	statePos.y  += stateNode.rect.height;
			// 	totalHeight += stateNode.rect.height;
			// }

			aMaxHeigth = ( totalHeight > aMaxHeigth )
				? totalHeight
				: aMaxHeigth;
		}

		private void UpdateGoalNodes () {
			for ( int i = 0, n = _goalNodes.Count; i < n; i++ ) {
				_goalNodes[ i ].IsHighlighted = _goalNodes[ i ].value.Equals ( _agent.GetGoal ().name );
			}
		}

		private void RebuildGoalNodes ( Vector2 aNodePosition, out float aMaxHeight ) {
			aMaxHeight = 0.0f;
			GoapAIDebuggerNode goalNode;
			for ( int i = 0, n = _agent.GetGoals ().Length; i < n; i++ ) {
				goalNode = CreateGoalNode ( _agent.GetGoals ()[ i ], _agent.getGoalState (), ref aNodePosition );
				aMaxHeight = ( goalNode.rect.height > aMaxHeight )
					? goalNode.rect.height
					: aMaxHeight;

				goalNode.value = _agent.GetGoals()[ i ].name;
				_goalNodes.Add ( goalNode );
			}
		}

		private void UpdatePlan ( Vector2 aNodePosition, out float aMaxHeight ) {
			for ( int i = 0, n = _genericNodes.Count; i < n; i++ ) {
				_genericNodes[ i ].isActive = false;
			}

			aMaxHeight = 0.0f;
			int                curIndex = 0;
			GoapAIDebuggerNode node;
			if ( _genericNodes.Count > 0 ) {
				node = _genericNodes[ curIndex ];
				UpdateWorldStateNode ( _agent, node );
				node.isActive   =  true;
				node.rect.x     =  aNodePosition.x;
				aNodePosition.x += node.rect.width;
			}
			else {
				node = CreateWorldStateNode ( _agent, ref aNodePosition );
			}

			if ( _currentPlan == null ) {
				return;
			}

			curIndex++;
			Action action;
			foreach ( var planAction in _currentPlan ) {
				action = planAction;
				if ( curIndex < _genericNodes.Count ) {
					node = _genericNodes[ curIndex ];
					UpdatePlanNode ( action, node );
					node.isActive   =  true;
					node.rect.x     =  aNodePosition.x;
					aNodePosition.x += node.rect.width;
				}
				else {
					node = CreatePlanNode ( action, ref aNodePosition );
				}

				aMaxHeight = ( node.rect.height > aMaxHeight )
					? node.rect.height
					: aMaxHeight;
				curIndex++;
			}
		}

		private void UpdateBlackboardNode () {
			// if ( _blackboardNode != null && _blackboard != null ) {
			// 	var desc = new List< string > ();
			// 	desc.Add ( string.Format ( "<b><color={0}>BLACKBOARD</color></b>", _titleColor ) );
			// 	desc.Add ( "   <b>Properties</b>" );
			//
			// 	DescribeBlackboard ( ref desc );
			//
			// 	StringBuilder text = new StringBuilder ();
			// 	for ( int i = 0, n = desc.Count; i < n; i++ ) {
			// 		text.AppendLine ( desc[ i ] );
			// 	}
			//
			// 	_blackboardNode.title       = text.ToString ();
			// 	_blackboardNode.rect.height = CalcHeight ( desc.Count );
			// }
		}

		private void RebuildBlackboardNode ( Vector2 aNodePosition ) {
			// if ( _blackboard != null ) {
			// 	var desc = new List< string > ();
			// 	desc.Add ( string.Format ( "<b><color={0}>BLACKBOARD</color></b>", _titleColor ) );
			// 	desc.Add ( "   <b>Properties</b>" );
			//
			// 	DescribeBlackboard ( ref desc );
			//
			// 	StringBuilder text = new StringBuilder ();
			// 	for ( int i = 0, n = desc.Count; i < n; i++ ) {
			// 		text.AppendLine ( desc[ i ] );
			// 	}
			//
			// 	_blackboardNode = AddNode ( text.ToString (), 440.0f, CalcHeight ( desc.Count ), _nodeStyle, _nodeStyle,
			// 		ref aNodePosition );
			// }
			// else {
			// 	var desc = string.Format (
			// 		"<b><color={0}>BLACKBOARD</color></b>\n\r   <color=white>Not exists!</color>", _titleColor );
			// 	_blackboardNode = AddNode ( desc, 440.0f, CalcHeight ( 2 ), _nodeStyle, _nodeStyle, ref aNodePosition );
			// }
		}

		private void DescribeBlackboard ( ref List< string > aResult ) {
			// if ( _blackboard != null ) {
			// 	bool value;
			// 	for ( int i = 0, n = _blackboard.Count; i < n; i++ ) {
			// 		if ( _blackboard[ i ].Type == AntAIBlackboardProp.ValueType.Bool ) {
			// 			value = _blackboard[ i ].AsBool;
			// 			aResult.Add ( string.Format ( "      '<color={3}>{0}</color>' = '<color={3}>{1}</color>' ({2})",
			// 				_blackboard.GetKey ( i ), _blackboard[ i ].ToString (), _blackboard[ i ].Type,
			// 				( value ) ? _trueColor : _falseColor ) );
			// 		}
			// 		else {
			// 			aResult.Add ( string.Format ( "      '<color={3}>{0}</color>' = '<color={3}>{1}</color>' ({2})",
			// 				_blackboard.GetKey ( i ), _blackboard[ i ].ToString (), _blackboard[ i ].Type,
			// 				_valueColor ) );
			// 		}
			// 	}
			// }
		}

		/// <summary>
		/// Создает заголовок.
		/// </summary>
		private void CreateTitle ( float aX, float aY, string aTitle ) {
			_titles.Add ( new TitleData () {
				text = aTitle,
				rect = new Rect ( aX, aY, 500.0f, 50.0f )
			} );
		}

		/// <summary>
		/// Создает ноду состояния информирующую о том что состояние не найдено.
		/// </summary>
		private GoapAIDebuggerNode CreateMissingStateNode ( string aTitle, Vector2 aNodePosition ) {
			return AddNode ( string.Format (
					"<b><color={1}>STATE</color> '<color={2}>{0}</color>'</b>\n\r   <color=white>Non-existent state!</color>",
					aTitle, _titleColor, _nameColor ),
				220.0f, 54.0f, _warningNodeStyle, _warningNodeStyle, ref aNodePosition );
		}

		/// <summary>
		/// Создает ноду состояния.
		/// </summary>
		private GoapAIDebuggerNode CreateStateNode ( string aState, Vector2 aNodePosition, bool isDefault ) {
			string title;
			float  height = 40.0f;
			if ( isDefault ) {
				title = string.Format (
					"<b><color={1}>STATE</color> '<color={2}>{0}</color>'</b>\n\r   <color=#51a9b0>This is Default state</color>",
					aState, _titleColor, _nameColor );
				height += 14.0f;
			}
			else {
				title = string.Format ( "<b><color={1}>STATE</color> '<color={2}>{0}</color>'</b>",
					aState, _titleColor, _nameColor );
			}

			GoapAIDebuggerNode node = AddNode ( title, 220.0f, height, _stateStyle, _stateStyle, ref aNodePosition );
			node.value = aState;
			return node;
		}

		private GoapAIDebuggerNode CreateActionNode ( Action aAction, ref Vector2 aNodePosition ) {
			bool value = false;
			var  desc  = new List< string > ();
			desc.Add ( string.Format ( "<b><color={2}>ACTION</color> '<color={3}>{0}</color>'</b> [{1}]",
				aAction.name, aAction.cost, _titleColor, _nameColor ) );
			desc.Add ( "   <b>Pre Conditions</b>" );

			foreach ( var set in aAction._preConditions ) {
				value = set.Item2;
				desc.Add ( string.Format ( "      '<color={2}>{0}</color>' = <color={2}>{1}</color>",
					set.Item1, value, ( value ) ? _trueColor : _falseColor ) );
			}

			desc.Add ( "   <b>Post Conditions</b>" );
			foreach ( var set in aAction._postConditions ) {
				value = set.Item2;
				desc.Add ( string.Format ( "      '<color={2}>{0}</color>' = <color={2}>{1}</color>",
					set.Item1, value, ( value ) ? _trueColor : _falseColor ) );
			}

			StringBuilder text = new StringBuilder ();
			for ( int i = 0, n = desc.Count; i < n; i++ ) {
				text.AppendLine ( desc[ i ] );
			}

			return AddNode ( text.ToString (), 220.0f, CalcHeight ( desc.Count ), _nodeStyle, _nodeStyle,
				ref aNodePosition );
		}

		/// <summary>
		/// Описывает состояние.
		/// </summary>
		private void DescribeCondition ( AIScenarioItem[] conditions, ref List< string > aResult ) {
			bool value;
			for ( int i = 0; i < conditions.Length; i++ ) {
				value = conditions[ i ].value;
				aResult.Add ( string.Format ( "      '<color={2}>{0}</color>' = <color={2}>{1}</color>",
					_agent.GetAIScenarioCondition ().GetName ( conditions[ i ].id ), value, ( value ) ? _trueColor : _falseColor ) );
			}
		}
		
		private void DescribeCurrentWorldState ( AIScenarioAgent agent, ref List< string > aResult ) {
			bool value;
			foreach ( var kv in agent.GetConditions () ) {
				if ( kv.Value == null ) {
					continue;
				}
				
				value = kv.Value.OnCheck ();
				aResult.Add ( string.Format ( "      '<color={2}>{0}</color>' = <color={2}>{1}</color>",
					kv.Key, value, ( value ) ? _trueColor : _falseColor ) );
			}
		}

		/// <summary>
		/// Создает новую ноду описывающую поставленную задачу (состояния к которым стримится ИИ).
		/// </summary>
		private GoapAIDebuggerNode CreateGoalNode ( AIScenarioGoal goal, WorldState goalSate, ref Vector2 aNodePosition ) {
			List< string > desc = new List< string > ();
			desc.Add ( string.Format ( "<b><color={1}>GOAL</color> '<color={2}>{0}</color>'</b>",
				goal.name, _titleColor, _nameColor ) );
			desc.Add ( "   <b>Tends to conditions</b>" );
			DescribeCondition ( goal.conditions, ref desc );

			StringBuilder text = new StringBuilder ();
			for ( int i = 0, n = desc.Count; i < n; i++ ) {
				text.AppendLine ( desc[ i ] );
			}

			return AddNode ( text.ToString (), 220.0f, CalcHeight ( desc.Count ), _goalStyle, _activeGoalStyle,
				ref aNodePosition );
		}

		/// <summary>
		/// Создает новую ноду описывающуюу текущее состояние мира (условия ИИ).
		/// </summary>
		private GoapAIDebuggerNode CreateWorldStateNode ( AIScenarioAgent agent, ref Vector2 aNodePosition ) {
			List< string > desc = new List< string > ();
			desc.Add ( string.Format ( "<b><color={0}>WORLD STATE</color></b>", _titleColor ) );
			desc.Add ( "   <b>Current Conditions</b>" );
			DescribeCurrentWorldState ( agent, ref desc );

			StringBuilder text = new StringBuilder ();
			for ( int i = 0, n = desc.Count; i < n; i++ ) {
				text.AppendLine ( desc[ i ] );
			}

			var node = AddNode ( text.ToString (), 220.0f, CalcHeight ( desc.Count ), _nodeStyle, _nodeStyle,
				ref aNodePosition, false );
			node.SetOutput ( node.rect.width - 10.0f, node.rect.height * 0.5f );
			node.SetInput ( 10.0f, node.rect.height * 0.5f );
			_genericNodes.Add ( node );
			return node;
		}

		/// <summary>
		/// Обновляет информацию уже существующей ноды описывающей состояние мира (условий ИИ).
		/// </summary>
		private void UpdateWorldStateNode ( AIScenarioAgent agent, GoapAIDebuggerNode aNode ) {
			List< string > desc = new List< string > ();
			desc.Add ( string.Format ( "<b><color={0}>WORLD STATE</color></b>", _titleColor ) );
			desc.Add ( "   <b>Current Conditions</b>" );
			DescribeCurrentWorldState ( agent, ref desc );

			StringBuilder text = new StringBuilder ();
			for ( int i = 0, n = desc.Count; i < n; i++ ) {
				text.AppendLine ( desc[ i ] );
			}

			aNode.title       = text.ToString ();
			aNode.rect.height = CalcHeight ( desc.Count );
		}

		/// <summary>
		/// Описывает конкретное действие из плана ИИ.
		/// </summary>
		private string DescribePlanAction ( AIScenarioAgent agent, Action action, out int aNumLines ) {
			var lines = new List< string > ();
			lines.Add ( string.Format ( "<b><color={1}>ACTION</color> '<color={2}>{0}</color>'</b>",
				action.name, _titleColor, _nameColor ) );
			lines.Add ( "   <b>Post Conditions</b>" );

			bool value;
			foreach ( var kv in agent.GetConditions () ) {
				if ( kv.Value == null ) {
					continue;
				}
				
				value = kv.Value.OnCheck ();

				bool diff = false;
				foreach ( var set in action._preConditions ) {
					if ( set.Item1 == kv.Key && set.Item2 != value ) {
						diff = true;
						value = set.Item2;
						break;
					}
				}
				foreach ( var set in action._postConditions ) {
					if ( set.Item1 == kv.Key && set.Item2 != value ) {
						diff = true;
						value = set.Item2;
						break;
					}
				}
				
				if ( diff ) {
					lines.Add ( string.Format (
						"      <color=#a873dd><b>></b></color> <i>'<color={2}>{0}</color>' = <color={2}>{1}</color></i>",
						kv.Key, value, value ? _trueColor : _falseColor ) );
				}
				else {
					lines.Add ( string.Format ( "      '<color={2}>{0}</color>' = <color={2}>{1}</color>",
						kv.Key, value, value ? _trueColor : _falseColor ) );
				}
			}

			StringBuilder text = new StringBuilder ();
			for ( int i = 0, n = lines.Count; i < n; i++ ) {
				text.AppendLine ( lines[ i ] );
			}

			aNumLines = lines.Count;
			return text.ToString ();
		}

		/// <summary>
		/// Создает новую ноду описывающую конкретное действие из плана ИИ.
		/// </summary>
		private GoapAIDebuggerNode CreatePlanNode ( Action aAction, ref Vector2    aNodePosition ) {
			
			int    numLines;
			string desc = DescribePlanAction ( _agent, aAction, out numLines );

			GUIStyle style = _planStyle;
			// if ( _currentPlan.isSuccess ) {
			// 	style = ( aAction.name.Equals ( _agent.currentPlan[ 0 ] ) ) ? _activePlanStyle : _planStyle;
			// }
			// else {
			// 	style = ( aAction.name.Equals ( _agent.currentPlan[ 0 ] ) ) ? _activeFailedPlanStyle : _failedPlanStyle;
			// }

			var node = AddNode ( desc, 220.0f, CalcHeight ( numLines ), style, style, ref aNodePosition, false );
			node.value = aAction.name;
			node.SetOutput ( node.rect.width - 10.0f, node.rect.height * 0.5f );
			node.SetInput ( 10.0f, node.rect.height * 0.5f );

			if ( _genericNodes.Count > 0 ) {
				_genericNodes[ _genericNodes.Count - 1 ].LinkTo ( node, new Color ( 0.3f, 0.7f, 0.4f ) );
			}

			_genericNodes.Add ( node );
			return node;
		}

		/// <summary>
		/// Обновляет ноду описывающую конкретное действие из плана ИИ.
		/// </summary>
		private void UpdatePlanNode ( Action aAction, GoapAIDebuggerNode aNode ) {
			aNode.value   = aAction.name;

			int numLines;
			aNode.title       = DescribePlanAction ( _agent, aAction, out numLines );
			aNode.rect.height = CalcHeight ( numLines );

			// if ( _currentPlan.isSuccess ) {
			// 	aNode.defaultNodeStyle =
			// 		( aAction.name.Equals ( _agent.currentPlan[ 0 ] ) ) ? _activePlanStyle : _planStyle;
			// }
			// else {
			// 	aNode.defaultNodeStyle = ( aAction.name.Equals ( _agent.currentPlan[ 0 ] ) )
			// 		? _activeFailedPlanStyle
			// 		: _failedPlanStyle;
			// }
			if ( _currentPlan != null && _currentPlan.Count != 0 ) {
				aNode.defaultNodeStyle = aAction == _currentPlan.Peek () ? _activePlanStyle : _planStyle;
			}
			else {
				aNode.defaultNodeStyle = _planStyle;
			}

			aNode.currentStyle = aNode.defaultNodeStyle;
		}

		/// <summary>
		/// Рассчитывает высоту ноды исходя из количества строк.
		/// </summary>
		private float CalcHeight ( int numLines ) {
			return 13.0f * ( float ) ( numLines + 1 ) + 14.0f;
		}

		// ---

		private GoapAIDebuggerNode AddNode ( string   aText,  float    aWidth,       float       aHeight,
		                                     GUIStyle aStyle, GUIStyle aActiveStyle, ref Vector2 aPosition,
		                                     bool     aAddToList = true ) {
			GoapAIDebuggerNode node = new GoapAIDebuggerNode ( aPosition.x, aPosition.y, aWidth, aHeight, aStyle, aActiveStyle );
			node.title = aText;
			if ( aAddToList ) {
				_nodes.Add ( node );
			}

			aPosition.x += aWidth;
			return node;
		}

		private void DrawTitles ( List< TitleData > aList ) {
			TitleData t;
			for ( int i = 0, n = aList.Count; i < n; i++ ) {
				t = aList[ i ];
				GUI.Label ( new Rect ( t.rect.x + _totalDrag.x, t.rect.y + _totalDrag.y, t.rect.width, t.rect.height ),
					t.text, _titleStyle );
			}
		}

		private void DrawLinks ( List< GoapAIDebuggerNode > aList, bool aVertical ) {
			GoapAIDebuggerNode node;
			for ( int i = 0, n = aList.Count; i < n; i++ ) {
				node = aList[ i ];
				if ( node.isActive && node.links.Count > 0 ) {
					for ( int j = 0, nj = node.links.Count; j < nj; j++ ) {
						if ( node.links[ j ].Key.isActive ) {
							if ( aVertical ) {
								Drawer.DrawVerticalSolidConnection ( node.Output,
									node.links[ j ].Key.Input, node.links[ j ].Value, 1, 15.0f );
							}
							else {
								Drawer.DrawSolidConnection ( node.Output,
									node.links[ j ].Key.Input, node.links[ j ].Value, 1, 15.0f );
							}
						}
					}
				}
			}
		}

		private void DrawCurrentStateLink () {
			// Реализация линка, что называется "в лоб" между текущим действием и состояним :)
			if ( _genericNodes.Count >= 2 ) {
				var                gNode = _genericNodes[ 1 ];
				GoapAIDebuggerNode sNode;
				for ( int i = 0, n = _nodes.Count; i < n; i++ ) {
					sNode = _nodes[ i ];
					if ( gNode.value.Equals ( sNode.value ) ) {
						Drawer.DrawVerticalSolidConnection (
							new Vector2 ( gNode.rect.x + gNode.rect.width * 0.5f, gNode.rect.y + 10.0f ),
							new Vector2 ( sNode.rect.x + gNode.rect.width * 0.5f,
								sNode.rect.y + sNode.rect.height - 10.0f ),
							Color.gray, -1, 50.0f );
						break;
					}
				}
			}
		}

		private void DrawNodes ( List< GoapAIDebuggerNode > aList ) {
			for ( int i = 0, n = aList.Count; i < n; i++ ) {
				if ( aList[ i ].isActive ) {
					aList[ i ].Draw ();
				}
			}
		}

		private void DrawGrid ( float aCellSize, Color aColor, float aOpacity ) {
			int cols = Mathf.CeilToInt ( position.width / aCellSize );
			int rows = Mathf.CeilToInt ( position.height / aCellSize );

			Handles.BeginGUI ();
			Color c = Handles.color;
			Handles.color = new Color ( aColor.r, aColor.g, aColor.b, aOpacity );

			_offset += _drag * 0.5f;
			Vector3 newOffset = new Vector3 ( _offset.x % aCellSize, _offset.y % aCellSize, 0.0f );

			for ( int i = 0; i < cols; i++ ) {
				Handles.DrawLine ( new Vector3 ( aCellSize * i, -aCellSize, 0.0f ) + newOffset,
					new Vector3 ( aCellSize * i, position.height, 0.0f ) + newOffset );
			}

			for ( int i = 0; i < rows; i++ ) {
				Handles.DrawLine ( new Vector3 ( -aCellSize, aCellSize * i, 0.0f ) + newOffset,
					new Vector3 ( position.width, aCellSize * i, 0.0f ) + newOffset );
			}

			Handles.color = c;
			Handles.EndGUI ();
		}

		private void ProcessEvents ( Event aEvent ) {
			_drag = Vector2.zero;

			for ( int i = 0; i < _nodes.Count; i++ ) {
				if ( _nodes[ i ].isActive &&
				     _nodes[ i ].ProcessEvents ( aEvent ) ) {
					return;
				}
			}

			for ( int i = 0, n = _genericNodes.Count; i < n; i++ ) {
				if ( _genericNodes[ i ].isActive &&
				     _genericNodes[ i ].ProcessEvents ( aEvent ) ) {
					return;
				}
			}

			switch ( aEvent.type ) {
				case EventType.MouseDrag :
					if ( aEvent.button == 0 ) {
						OnDrag ( aEvent.delta );
					}

					break;
			}
		}

		private void OnDrag ( Vector2 aDelta ) {
			_totalDrag += aDelta;
			_drag      =  aDelta;

			for ( int i = 0, n = _nodes.Count; i < n; i++ ) {
				if ( _nodes[ i ].isActive ) {
					_nodes[ i ].Drag ( aDelta );
				}
			}

			for ( int i = 0, n = _genericNodes.Count; i < n; i++ ) {
				if ( _genericNodes[ i ].isActive ) {
					_genericNodes[ i ].Drag ( aDelta );
				}
			}

			GUI.changed = true;
		}

		#endregion
	}
}
