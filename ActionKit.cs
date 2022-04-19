/****************************************************************************
 * Copyright (c) 2018 ~ 2022 liangxiegame UNDER MIT LICENSE
 * 
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 *
 * Latest Update: 2021.2.15 14:18 Add Simple ECA Pattern
 ****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace QFramework
{
    public class ActionKit
    {
        [RuntimeInitializeOnLoadMethod]
        private static void InitNodeSystem()
        {
            // cache list			

            // cache node
            SafeObjectPool<DelayAction>.Instance.Init(50, 50);
            SafeObjectPool<EventAction>.Instance.Init(50, 50);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// 延时执行节点
    /// </summary>
    [Serializable]
    [ActionGroup("ActionKit")]
    public class DelayAction : ActionKitAction, IPoolable, IResetable
    {
        [SerializeField] public float DelayTime;

        public System.Action OnDelayFinish { get; set; }

        public float CurrentSeconds { get; set; }

        public static DelayAction Allocate(float delayTime, System.Action onDelayFinish = null)
        {
            var retNode = SafeObjectPool<DelayAction>.Instance.Allocate();
            retNode.DelayTime = delayTime;
            retNode.OnDelayFinish = onDelayFinish;
            retNode.CurrentSeconds = 0.0f;
            return retNode;
        }

        protected override void OnReset()
        {
            CurrentSeconds = 0.0f;
        }

        protected override void OnExecute(float dt)
        {
            CurrentSeconds += dt;
            Finished = CurrentSeconds >= DelayTime;
            if (Finished && OnDelayFinish != null)
            {
                OnDelayFinish();
            }
        }

        protected override void OnDispose()
        {
            SafeObjectPool<DelayAction>.Instance.Recycle(this);
        }

        public void OnRecycled()
        {
            OnDelayFinish = null;
            DelayTime = 0.0f;
            Reset();
        }

        public bool IsRecycled { get; set; }
    }

    public static class DelayActionExtensions
    {
        public static IAction Delay<T>(this T selfBehaviour, float seconds, System.Action delayEvent)
            where T : MonoBehaviour
        {
            var delayAction = DelayAction.Allocate(seconds, delayEvent);
            selfBehaviour.ExecuteNode(delayAction);
            return delayAction;
        }

        public static IActionChain Delay(this IActionChain selfChain, float seconds)
        {
            return selfChain.Append(DelayAction.Allocate(seconds));
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// 安枕执行节点
    /// </summary>
    [Serializable]
    [ActionGroup("ActionKit")]
    public class DelayFrameAction : ActionKitAction, IPoolable, IResetable
    {
        [SerializeField] public int FrameCount;

        public System.Action OnDelayFrameFinish { get; set; }

        public float CurrentSeconds { get; set; }

        public static DelayFrameAction Allocate(int frameCount, System.Action onDelayFrameFinish = null)
        {
            var retNode = SafeObjectPool<DelayFrameAction>.Instance.Allocate();
            retNode.FrameCount = frameCount;
            retNode.OnDelayFrameFinish = onDelayFrameFinish;
            retNode.CurrentSeconds = 0.0f;
            return retNode;
        }

        protected override void OnReset()
        {
            CurrentSeconds = 0.0f;
        }

        private int StartFrame;

        protected override void OnBegin()
        {
            base.OnBegin();

            StartFrame = Time.frameCount;
        }

        protected override void OnExecute(float dt)
        {
            Finished = Time.frameCount - StartFrame >= FrameCount;

            if (Finished && OnDelayFrameFinish != null)
            {
                OnDelayFrameFinish();
            }
        }

        protected override void OnDispose()
        {
            SafeObjectPool<DelayFrameAction>.Instance.Recycle(this);
        }

        public void OnRecycled()
        {
            OnDelayFrameFinish = null;
            FrameCount = 0;
            Reset();
        }

        public bool IsRecycled { get; set; }
    }

    public static class DelayFrameActionExtensions
    {
        public static void DelayFrame<T>(this T selfBehaviour, int frameCount, System.Action delayFrameEvent)
            where T : MonoBehaviour
        {
            selfBehaviour.ExecuteNode(DelayFrameAction.Allocate(frameCount, delayFrameEvent));
        }

        public static void NextFrame<T>(this T selfBehaviour, System.Action nextFrameEvent)
            where T : MonoBehaviour
        {
            selfBehaviour.ExecuteNode(DelayFrameAction.Allocate(1, nextFrameEvent));
        }

        public static IActionChain DelayFrame(this IActionChain selfChain, int frameCount)
        {
            return selfChain.Append(DelayFrameAction.Allocate(frameCount));
        }

        public static IActionChain NextFrame(this IActionChain selfChain)
        {
            return selfChain.Append(DelayFrameAction.Allocate(1));
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// 延时执行节点
    /// </summary>
    [OnlyUsedByCode]
    public class EventAction : ActionKitAction, IPoolable
    {
        private System.Action mOnExecuteEvent;

        /// <summary>
        /// TODO:这里填可变参数会有问题
        /// </summary>
        /// <param name="onExecuteEvents"></param>
        /// <returns></returns>
        public static EventAction Allocate(params System.Action[] onExecuteEvents)
        {
            var retNode = SafeObjectPool<EventAction>.Instance.Allocate();
            Array.ForEach(onExecuteEvents, onExecuteEvent => retNode.mOnExecuteEvent += onExecuteEvent);
            return retNode;
        }

        /// <summary>
        /// finished
        /// </summary>
        protected override void OnExecute(float dt)
        {
            if (mOnExecuteEvent != null)
            {
                mOnExecuteEvent.Invoke();
            }

            Finished = true;
        }

        protected override void OnDispose()
        {
            SafeObjectPool<EventAction>.Instance.Recycle(this);
        }

        public void OnRecycled()
        {
            Reset();
            mOnExecuteEvent = null;
        }

        public bool IsRecycled { get; set; }
    }

    [OnlyUsedByCode]
    public class OnlyBeginAction : ActionKitAction, IPoolable, IPoolType
    {
        private Action<OnlyBeginAction> mBeginAction;

        public static OnlyBeginAction Allocate(Action<OnlyBeginAction> beginAction)
        {
            var retSimpleAction = SafeObjectPool<OnlyBeginAction>.Instance.Allocate();

            retSimpleAction.mBeginAction = beginAction;

            return retSimpleAction;
        }

        public void OnRecycled()
        {
            mBeginAction = null;
        }

        protected override void OnBegin()
        {
            if (mBeginAction != null)
            {
                mBeginAction.Invoke(this);
            }
        }

        public bool IsRecycled { get; set; }

        public void Recycle2Cache()
        {
            SafeObjectPool<OnlyBeginAction>.Instance.Recycle(this);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// like filter, add condition
    /// </summary>
    [OnlyUsedByCode]
    public class UntilAction : ActionKitAction, IPoolable
    {
        private Func<bool> mCondition;

        public static UntilAction Allocate(Func<bool> condition)
        {
            var retNode = SafeObjectPool<UntilAction>.Instance.Allocate();
            retNode.mCondition = condition;
            return retNode;
        }

        protected override void OnExecute(float dt)
        {
            Finished = mCondition.Invoke();
        }

        protected override void OnDispose()
        {
            SafeObjectPool<UntilAction>.Instance.Recycle(this);
        }

        void IPoolable.OnRecycled()
        {
            Reset();
            mCondition = null;
        }

        bool IPoolable.IsRecycled { get; set; }
    }

    internal class OnDestroyDisposeTrigger : MonoBehaviour
    {
        HashSet<IDisposable> mDisposables = new HashSet<IDisposable>();

        public void AddDispose(IDisposable disposable)
        {
            if (!mDisposables.Contains(disposable))
            {
                mDisposables.Add(disposable);
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                foreach (var disposable in mDisposables)
                {
                    disposable.Dispose();
                }

                mDisposables.Clear();
                mDisposables = null;
            }
        }
    }

    internal class OnDisableDisposeTrigger : MonoBehaviour
    {
        HashSet<IDisposable> mDisposables = new HashSet<IDisposable>();

        public void AddDispose(IDisposable disposable)
        {
            if (!mDisposables.Contains(disposable))
            {
                mDisposables.Add(disposable);
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                foreach (var disposable in mDisposables)
                {
                    disposable.Dispose();
                }

                mDisposables.Clear();
                mDisposables = null;
            }
        }
    }

    internal static class IDisposableExtensions
    {
        /// <summary>
        /// 与 GameObject 绑定销毁
        /// </summary>
        /// <param name="self"></param>
        /// <param name="component"></param>
        public static void DisposeWhenGameObjectDestroyed(this IDisposable self, Component component)
        {
            var onDestroyDisposeTrigger = component.gameObject.GetComponent<OnDestroyDisposeTrigger>();
            if (!onDestroyDisposeTrigger)
            {
                onDestroyDisposeTrigger = component.gameObject.AddComponent<OnDestroyDisposeTrigger>();
            }

            onDestroyDisposeTrigger.AddDispose(self);
        }
    }

    /// <summary>
    /// 时间轴执行节点
    /// </summary>
    public class Timeline : ActionKitAction
    {
        private float mCurTime = 0;

        public System.Action OnTimelineBeganCallback
        {
            get { return OnBeganCallback; }
            set { OnBeganCallback = value; }
        }

        public System.Action OnTimelineEndedCallback
        {
            get { return OnEndedCallback; }
            set { OnEndedCallback = value; }
        }

        public Action<string> OnKeyEventsReceivedCallback = null;

        public class TimelinePair
        {
            public float Time;
            public IAction Node;

            public TimelinePair(float time, IAction node)
            {
                Time = time;
                Node = node;
            }
        }

        /// <summary>
        /// refator 2 one list? all in one list;
        /// </summary>
        public Queue<TimelinePair> TimelineQueue = new Queue<TimelinePair>();

        protected override void OnReset()
        {
            mCurTime = 0.0f;

            foreach (var timelinePair in TimelineQueue)
            {
                timelinePair.Node.Reset();
            }
        }

        protected override void OnExecute(float dt)
        {
            mCurTime += dt;

            foreach (var pair in TimelineQueue.Where(pair => pair.Time < mCurTime && !pair.Node.Finished))
            {
                if (pair.Node.Execute(dt))
                {
                    Finished = TimelineQueue.Count(timelinePair => !timelinePair.Node.Finished) == 0;
                }
            }
        }

        public Timeline(params TimelinePair[] pairs)
        {
            foreach (var pair in pairs)
            {
                TimelineQueue.Enqueue(pair);
            }
        }

        public void Append(TimelinePair pair)
        {
            TimelineQueue.Enqueue(pair);
        }

        public void Append(float time, IAction node)
        {
            TimelineQueue.Enqueue(new TimelinePair(time, node));
        }

        protected override void OnDispose()
        {
            foreach (var timelinePair in TimelineQueue)
            {
                timelinePair.Node.Dispose();
            }

            TimelineQueue.Clear();
            TimelineQueue = null;
        }
    }

    public class KeyEventAction : ActionKitAction, IPoolable
    {
        private Timeline mTimeline;
        private string mEventName;

        public static KeyEventAction Allocate(string eventName, Timeline timeline)
        {
            var keyEventAction = SafeObjectPool<KeyEventAction>.Instance.Allocate();

            keyEventAction.mEventName = eventName;
            keyEventAction.mTimeline = timeline;

            return keyEventAction;
        }

        protected override void OnBegin()
        {
            base.OnBegin();

            mTimeline.OnKeyEventsReceivedCallback?.Invoke(mEventName);

            Finish();
        }

        protected override void OnDispose()
        {
            SafeObjectPool<KeyEventAction>.Instance.Recycle(this);
        }

        public void OnRecycled()
        {
            mTimeline = null;
            mEventName = null;
        }

        public bool IsRecycled { get; set; }
    }

    public class AsyncNode : ActionKitAction
    {
        public HashSet<IAction> mActions = new HashSet<IAction>();

        public void Add(IAction action)
        {
            mActions.Add(action);
        }

        protected override void OnExecute(float dt)
        {
            foreach (var action in mActions.Where(action => action.Execute(dt)))
            {
                mActions.Remove(action);
                action.Dispose();
            }
        }
    }

    public class QueueNode : ActionKitAction
    {
        private Queue<IAction> mQueue = new Queue<IAction>(20);

        public void Enqueue(IAction action)
        {
            mQueue.Enqueue(action);
        }

        protected override void OnExecute(float dt)
        {
            if (mQueue.Count != 0 && mQueue.Peek().Execute(dt))
            {
                mQueue.Dequeue().Dispose();
            }
        }
    }

    public interface IResetable
    {
        void Reset();
    }

    [OnlyUsedByCode]
    public class RepeatNode : ActionKitAction, INode
    {
        public RepeatNode(IAction node, int repeatCount = -1)
        {
            RepeatCount = repeatCount;
            mNode = node;
        }

        public IAction CurrentExecutingNode
        {
            get
            {
                var currentNode = mNode;
                var node = currentNode as INode;
                return node == null ? currentNode : node.CurrentExecutingNode;
            }
        }

        private IAction mNode;

        public int RepeatCount = 1;

        private int mCurRepeatCount = 0;

        protected override void OnReset()
        {
            if (null != mNode)
            {
                mNode.Reset();
            }

            mCurRepeatCount = 0;
            Finished = false;
        }

        protected override void OnExecute(float dt)
        {
            if (RepeatCount == -1)
            {
                if (mNode.Execute(dt))
                {
                    mNode.Reset();
                }

                return;
            }

            if (mNode.Execute(dt))
            {
                mNode.Reset();
                mCurRepeatCount++;
            }

            if (mCurRepeatCount == RepeatCount)
            {
                Finished = true;
            }
        }

        protected override void OnDispose()
        {
            if (null != mNode)
            {
                mNode.Dispose();
                mNode = null;
            }
        }
    }

    /// <summary>
    /// 序列执行节点
    /// </summary>
    [OnlyUsedByCode]
    public class SequenceNode : ActionKitAction, INode, IResetable
    {
        protected List<IAction> mNodes = ListPool<IAction>.Get();
        protected List<IAction> mExcutingNodes = ListPool<IAction>.Get();

        public int TotalCount
        {
            get { return mExcutingNodes.Count; }
        }

        public IAction CurrentExecutingNode
        {
            get
            {
                var currentNode = mExcutingNodes[0];
                var node = currentNode as INode;
                return node == null ? currentNode : node.CurrentExecutingNode;
            }
        }

        protected override void OnReset()
        {
            mExcutingNodes.Clear();
            foreach (var node in mNodes)
            {
                node.Reset();
                mExcutingNodes.Add(node);
            }
        }

        protected override void OnExecute(float dt)
        {
            if (mExcutingNodes.Count > 0)
            {
                // 如果有异常，则进行销毁，不再进行下边的操作
                if (mExcutingNodes[0].Disposed && !mExcutingNodes[0].Finished)
                {
                    Dispose();
                    return;
                }

                while (mExcutingNodes[0].Execute(dt))
                {
                    mExcutingNodes.RemoveAt(0);

                    OnCurrentActionFinished();

                    if (mExcutingNodes.Count == 0)
                    {
                        break;
                    }
                }
            }

            Finished = mExcutingNodes.Count == 0;
        }

        protected virtual void OnCurrentActionFinished()
        {
        }

        public SequenceNode(params IAction[] nodes)
        {
            foreach (var node in nodes)
            {
                mNodes.Add(node);
                mExcutingNodes.Add(node);
            }
        }

        public SequenceNode Append(IAction appendedNode)
        {
            mNodes.Add(appendedNode);
            mExcutingNodes.Add(appendedNode);
            return this;
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            if (null != mNodes)
            {
                mNodes.ForEach(node => node.Dispose());
                mNodes.Clear();
                mNodes.Release2Pool();
                mNodes = null;
            }

            if (null != mExcutingNodes)
            {
                mExcutingNodes.Release2Pool();
                mExcutingNodes = null;
            }
        }
    }

    /// <summary>
    /// 并发执行的协程
    /// </summary>
    [OnlyUsedByCode]
    public class SpawnNode : ActionKitAction
    {
        protected List<ActionKitAction> mNodes = ListPool<ActionKitAction>.Get();

        protected override void OnReset()
        {
            mNodes.ForEach(node => node.Reset());
            mFinishCount = 0;
        }

        public override void Finish()
        {
            for (var i = mNodes.Count - 1; i >= 0; i--)
            {
                mNodes[i].Finish();
            }

            base.Finish();
        }

        protected override void OnExecute(float dt)
        {
            for (var i = mNodes.Count - 1; i >= 0; i--)
            {
                var node = mNodes[i];
                if (!node.Finished && node.Execute(dt))
                    Finished = mNodes.Count == mFinishCount;
            }
        }

        private int mFinishCount = 0;

        private void IncreaseFinishCount()
        {
            mFinishCount++;
        }

        public SpawnNode(params ActionKitAction[] nodes)
        {
            mNodes.AddRange(nodes);

            foreach (var nodeAction in nodes)
            {
                nodeAction.OnEndedCallback += IncreaseFinishCount;
            }
        }

        public void Add(params ActionKitAction[] nodes)
        {
            mNodes.AddRange(nodes);

            foreach (var nodeAction in nodes)
            {
                nodeAction.OnEndedCallback += IncreaseFinishCount;
            }
        }

        protected override void OnDispose()
        {
            foreach (var node in mNodes)
            {
                node.OnEndedCallback -= IncreaseFinishCount;
                node.Dispose();
            }

            mNodes.Release2Pool();
            mNodes = null;
        }
    }

    public class ActionKitFSM
    {
        public ActionKitFSMState CurrentState { get; private set; }
        public Type PreviousStateType { get; private set; }

        public void Update()
        {
            if (CurrentState != null)
            {
                CurrentState.Update();
            }
        }

        public void FixedUpdate()
        {
            if (CurrentState != null)
            {
                CurrentState.FixedUpdate();
            }
        }

        private Dictionary<Type, ActionKitFSMState> mStates = new Dictionary<Type, ActionKitFSMState>();
        private ActionKitFSMTransitionTable mTrasitionTable = new ActionKitFSMTransitionTable();


        public void AddState(ActionKitFSMState state)
        {
            mStates.Add(state.GetType(), state);
        }

        public void AddTransition(ActionKitFSMTransition transition)
        {
            mTrasitionTable.Add(transition);
        }

        public bool HandleEvent<TTransition>() where TTransition : ActionKitFSMTransition
        {
            foreach (var transition in mTrasitionTable.TypeIndex.Get(typeof(TTransition)))
            {
                if (transition.SrcStateTypes.Contains(CurrentState.GetType()))
                {
                    var currentState = CurrentState;
                    var nextState = mStates[transition.DstStateType];
                    CurrentState.Exit();
                    transition.OnTransition(currentState, nextState);
                    CurrentState = nextState;
                    CurrentState.Enter();
                    return true;
                }
            }

            return false;
        }

        public void ChangeState<TState>() where TState : ActionKitFSMState
        {
            ChangeState(typeof(TState));
        }

        public void ChangeState(Type stateType)
        {
            if (CurrentState.GetType() != stateType)
            {
                PreviousStateType = CurrentState.GetType();
                CurrentState.Exit();
                CurrentState = mStates[stateType];
                CurrentState.Enter();
            }
        }

        public void BackToPreviousState()
        {
            ChangeState(PreviousStateType);
        }

        public void StartState<T>()
        {
            CurrentState = mStates[typeof(T)];
            CurrentState.Enter();
        }
    }

    public class ActionKitFSMState
    {
        public void Enter()
        {
            OnEnter();
        }

        public void Update()
        {
            OnUpdate();
        }

        public void FixedUpdate()
        {
            OnFixedUpdate();
        }

        public void Exit()
        {
            OnExit();
        }

        protected virtual void OnEnter()
        {
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnFixedUpdate()
        {
        }

        protected virtual void OnExit()
        {
        }
    }

    public class ActionKitFSMState<T> : ActionKitFSMState
    {
        public ActionKitFSMState(T target)
        {
            mTarget = target;
        }

        protected T mTarget;

        public T Target
        {
            get { return mTarget; }
        }
    }

    public class ActionKitFSMTransition
    {
        public virtual HashSet<Type> SrcStateTypes { get; set; }

        public virtual Type DstStateType { get; set; }

        public virtual void OnTransition(ActionKitFSMState srcState, ActionKitFSMState dstState)
        {
        }

        public ActionKitFSMTransition AddSrcState<T>() where T : ActionKitFSMState
        {
            if (SrcStateTypes == null)
            {
                SrcStateTypes = new HashSet<Type>()
                {
                    typeof(T)
                };
            }
            else
            {
                SrcStateTypes.Add(typeof(T));
            }

            return this;
        }

        public ActionKitFSMTransition WithDstState<T>() where T : ActionKitFSMState
        {
            DstStateType = typeof(T);
            return this;
        }
    }


    public class ActionKitFSMTransition<TSrcState, TDstState> : ActionKitFSMTransition
    {
        private Type mSrcStateType = typeof(TSrcState);
        private Type mDstStateType = typeof(TDstState);

        public override HashSet<Type> SrcStateTypes
        {
            get { return new HashSet<Type>() { mSrcStateType }; }
        }

        public override Type DstStateType
        {
            get { return mDstStateType; }
        }
    }

    public struct EnumStateChangeEvent<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        public GameObject Target;
        public EnumStateMachine<T> TargetStateMachine;
        public T NewState;
        public T PreviousState;

        public EnumStateChangeEvent(EnumStateMachine<T> stateMachine)
        {
            Target = stateMachine.Target;
            TargetStateMachine = stateMachine;
            NewState = stateMachine.CurrentState;
            PreviousState = stateMachine.PreviousState;
        }
    }

    public interface IEnumStateMachine
    {
        bool TriggerEvents { get; set; }
    }

    public class EnumStateMachine<T> : IEnumStateMachine where T : struct, IComparable, IConvertible, IFormattable
    {
        /// If you set TriggerEvents to true, the state machine will trigger events when entering and exiting a state. 
        /// Additionnally, if you also use a StateMachineProcessor, it'll trigger events for the current state on FixedUpdate, LateUpdate, but also
        /// on Update (separated in EarlyUpdate, Update and EndOfUpdate, triggered in this order at Update()
        /// To listen to these events, from any class, in its Start() method (or wherever you prefer), use MMEventManager.StartListening(gameObject.GetInstanceID().ToString()+"XXXEnter",OnXXXEnter);
        /// where XXX is the name of the state you're listening to, and OnXXXEnter is the method you want to call when that event is triggered.
        /// MMEventManager.StartListening(gameObject.GetInstanceID().ToString()+"CrouchingEarlyUpdate",OnCrouchingEarlyUpdate); for example will listen to the Early Update event of the Crouching state, and 
        /// will trigger the OnCrouchingEarlyUpdate() method. 
        public bool TriggerEvents { get; set; }

        /// the name of the target gameobject
        public GameObject Target;

        /// the current character's movement state
        public T CurrentState { get; protected set; }

        /// the character's movement state before entering the current one
        public T PreviousState { get; protected set; }

        /// <summary>
        /// Creates a new StateMachine, with a targetName (used for events, usually use GetInstanceID()), and whether you want to use events with it or not
        /// </summary>
        /// <param name="targetName">Target name.</param>
        /// <param name="triggerEvents">If set to <c>true</c> trigger events.</param>
        public EnumStateMachine(GameObject target, bool triggerEvents)
        {
            this.Target = target;
            this.TriggerEvents = triggerEvents;
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="newState">New state.</param>
        public virtual void ChangeState(T newState)
        {
            // if the "new state" is the current one, we do nothing and exit
            if (newState.Equals(CurrentState))
            {
                return;
            }

            // we store our previous character movement state
            PreviousState = CurrentState;
            CurrentState = newState;

            if (TriggerEvents)
            {
                // TypeEventSystem.Global.Send(new EnumStateChangeEvent<T>(this));
            }
        }

        /// <summary>
        /// 返回上一个状态
        /// </summary>
        public virtual void RestorePreviousState()
        {
            CurrentState = PreviousState;

            if (TriggerEvents)
            {
                // TypeEventSystem.Global.Send(new EnumStateChangeEvent<T>(this));
            }
        }
    }

    public class ActionKitFSMTransitionTable : ActionKitTable<ActionKitFSMTransition>
    {
        public ActionKitTableIndex<Type, ActionKitFSMTransition> TypeIndex =
            new ActionKitTableIndex<Type, ActionKitFSMTransition>(t => t.GetType());

        protected override void OnAdd(ActionKitFSMTransition item)
        {
            TypeIndex.Add(item);
        }

        protected override void OnRemove(ActionKitFSMTransition item)
        {
            TypeIndex.Remove(item);
        }

        protected override void OnClear()
        {
            TypeIndex.Clear();
        }

        public override IEnumerator<ActionKitFSMTransition> GetEnumerator()
        {
            return TypeIndex.Dictionary.SelectMany(src => src.Value).GetEnumerator();
        }

        protected override void OnDispose()
        {
            TypeIndex.Dispose();
        }
    }

    /// <summary>
    /// FSM 基于枚举的状态机
    /// </summary>
    public class FSM<TStateEnum, TEventEnum> : IDisposable
    {
        private Action<TStateEnum, TStateEnum> mOnStateChanged = null;

        public FSM(Action<TStateEnum, TStateEnum> onStateChanged = null)
        {
            mOnStateChanged = onStateChanged;
        }

        /// <summary>
        /// FSM onStateChagned.
        /// </summary>
        public delegate void FSMOnStateChanged(params object[] param);

        /// <summary>
        /// QFSM state.
        /// </summary>
        public class FSMState<TName>
        {
            public TName Name;

            public FSMState(TName name)
            {
                Name = name;
            }

            /// <summary>
            /// The translation dict.
            /// </summary>
            public readonly Dictionary<TEventEnum, FSMTranslation<TName, TEventEnum>> TranslationDict =
                new Dictionary<TEventEnum, FSMTranslation<TName, TEventEnum>>();
        }

        /// <summary>
        /// Translation 
        /// </summary>
        public class FSMTranslation<TStateName, KEventName>
        {
            public TStateName FromState;
            public KEventName Name;
            public TStateName ToState;
            public Action<object[]> OnTranslationCallback; // 回调函数

            public FSMTranslation(TStateName fromState, KEventName name, TStateName toState,
                Action<object[]> onStateChagned)
            {
                FromState = fromState;
                ToState = toState;
                Name = name;
                OnTranslationCallback = onStateChagned;
            }
        }

        /// <summary>
        /// The state of the m current.
        /// </summary>
        TStateEnum mCurState;

        public TStateEnum State
        {
            get { return mCurState; }
        }

        /// <summary>
        /// The m state dict.
        /// </summary>
        Dictionary<TStateEnum, FSMState<TStateEnum>>
            mStateDict = new Dictionary<TStateEnum, FSMState<TStateEnum>>();

        /// <summary>
        /// Adds the state.
        /// </summary>
        /// <param name="name">Name.</param>
        private void AddState(TStateEnum name)
        {
            mStateDict[name] = new FSMState<TStateEnum>(name);
        }

        /// <summary>
        /// Adds the translation.
        /// </summary>
        /// <param name="fromState">From state.</param>
        /// <param name="name">Name.</param>
        /// <param name="toState">To state.</param>
        /// <param name="onStateChagned">Callfunc.</param>
        public void AddTransition(TStateEnum fromState, TEventEnum name, TStateEnum toState,
            Action<object[]> onStateChagned = null)
        {
            if (!mStateDict.ContainsKey(fromState))
            {
                AddState(fromState);
            }

            if (!mStateDict.ContainsKey(toState))
            {
                AddState(toState);
            }

            mStateDict[fromState].TranslationDict[name] =
                new FSMTranslation<TStateEnum, TEventEnum>(fromState, name, toState, onStateChagned);
        }

        /// <summary>
        /// Start the specified name.
        /// </summary>
        /// <param name="name">Name.</param>
        public void Start(TStateEnum name)
        {
            mCurState = name;
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="param">Parameter.</param>
        public void HandleEvent(TEventEnum name, params object[] param)
        {
            if (mCurState != null && mStateDict[mCurState].TranslationDict.ContainsKey(name))
            {
                var tempTranslation = mStateDict[mCurState].TranslationDict[name];

                if (tempTranslation.OnTranslationCallback != null)
                {
                    tempTranslation.OnTranslationCallback.Invoke(param);
                }

                if (mOnStateChanged != null)
                {
                    mOnStateChanged.Invoke(mCurState, tempTranslation.ToState);
                }

                mCurState = tempTranslation.ToState;
            }
        }

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public void Clear()
        {
            foreach (var keyValuePair in mStateDict)
            {
                foreach (var translationDictValue in keyValuePair.Value.TranslationDict.Values)
                {
                    translationDictValue.OnTranslationCallback = null;
                }

                keyValuePair.Value.TranslationDict.Clear();
            }

            mStateDict.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }

    /// <summary>
    /// QFSM lite.
    /// 基于字符串的状态机
    /// </summary>
    public class QFSMLite
    {
        /// <summary>
        /// FSM callfunc.
        /// </summary>
        public delegate void FSMCallfunc(params object[] param);

        /// <summary>
        /// QFSM state.
        /// </summary>
        class QFSMState
        {
            public string Name;

            public QFSMState(string name)
            {
                Name = name;
            }

            /// <summary>
            /// The translation dict.
            /// </summary>
            public readonly Dictionary<string, QFSMTranslation> TranslationDict =
                new Dictionary<string, QFSMTranslation>();
        }

        /// <summary>
        /// Translation 
        /// </summary>
        public class QFSMTranslation
        {
            public string FromState;
            public string Name;
            public string ToState;
            public FSMCallfunc OnTranslationCallback; // 回调函数

            public QFSMTranslation(string fromState, string name, string toState, FSMCallfunc onTranslationCallback)
            {
                FromState = fromState;
                ToState = toState;
                Name = name;
                OnTranslationCallback = onTranslationCallback;
            }
        }

        /// <summary>
        /// The state of the m current.
        /// </summary>
        string mCurState;

        public string State
        {
            get { return mCurState; }
        }

        /// <summary>
        /// The m state dict.
        /// </summary>
        Dictionary<string, QFSMState> mStateDict = new Dictionary<string, QFSMState>();

        /// <summary>
        /// Adds the state.
        /// </summary>
        /// <param name="name">Name.</param>
        public void AddState(string name)
        {
            mStateDict[name] = new QFSMState(name);
        }

        /// <summary>
        /// Adds the translation.
        /// </summary>
        /// <param name="fromState">From state.</param>
        /// <param name="name">Name.</param>
        /// <param name="toState">To state.</param>
        /// <param name="callfunc">Callfunc.</param>
        public void AddTranslation(string fromState, string name, string toState, FSMCallfunc callfunc)
        {
            mStateDict[fromState].TranslationDict[name] = new QFSMTranslation(fromState, name, toState, callfunc);
        }

        /// <summary>
        /// Start the specified name.
        /// </summary>
        /// <param name="name">Name.</param>
        public void Start(string name)
        {
            mCurState = name;
        }

        /// <summary>
        /// Handles the event.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="param">Parameter.</param>
        public void HandleEvent(string name, params object[] param)
        {
            if (mCurState != null && mStateDict[mCurState].TranslationDict.ContainsKey(name))
            {
                var tempTranslation = mStateDict[mCurState].TranslationDict[name];
                tempTranslation.OnTranslationCallback(param);
                mCurState = tempTranslation.ToState;
            }
        }

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public void Clear()
        {
            mStateDict.Clear();
        }
    }

    public interface IAction : IDisposable
    {
        bool Disposed { get; }

        bool Execute(float delta);

        void Reset();

        void Finish();

        bool Finished { get; }
    }

    [System.Serializable]
    public class ActionData
    {
        [SerializeField] public string ActionName;
        [SerializeField] public string AcitonData;
    }

    public interface INode
    {
        IAction CurrentExecutingNode { get; }
    }

    [Serializable]
    public abstract class ActionKitAction : IAction
    {
        public System.Action OnBeganCallback = null;
        public System.Action OnEndedCallback = null;
        public System.Action OnDisposedCallback = null;

        protected bool mOnBeginCalled = false;

        #region IAction Support

        bool IAction.Disposed
        {
            get { return mDisposed; }
        }

        protected bool mDisposed = false;

        public bool Finished { get; protected set; }

        public virtual void Finish()
        {
            Finished = true;
        }

        public void Break()
        {
            Finished = true;
        }

        #endregion

        #region ResetableSupport

        public void Reset()
        {
            Finished = false;
            mOnBeginCalled = false;
            mDisposed = false;
            OnReset();
        }

        #endregion


        #region IExecutable Support

        public bool Execute(float dt)
        {
            if (mDisposed) return true;

            // 有可能被别的地方调用
            if (Finished)
            {
                return Finished;
            }

            if (!mOnBeginCalled)
            {
                mOnBeginCalled = true;
                OnBegin();

                if (OnBeganCallback != null)
                {
                    OnBeganCallback.Invoke();
                }
            }

            if (!Finished)
            {
                OnExecute(dt);
            }

            if (Finished)
            {
                if (OnEndedCallback != null)
                {
                    OnEndedCallback.Invoke();
                }

                OnEnd();
            }

            return Finished || mDisposed;
        }

        #endregion

        protected virtual void OnReset()
        {
        }

        protected virtual void OnBegin()
        {
        }

        /// <summary>
        /// finished
        /// </summary>
        protected virtual void OnExecute(float dt)
        {
        }

        protected virtual void OnEnd()
        {
        }

        protected virtual void OnDispose()
        {
        }

        #region IDisposable Support

        public void Dispose()
        {
            if (mDisposed) return;
            mDisposed = true;

            OnBeganCallback = null;
            OnEndedCallback = null;

            if (OnDisposedCallback != null)
            {
                OnDisposedCallback.Invoke();
            }

            OnDisposedCallback = null;
            OnDispose();
        }

        #endregion
    }

    public class OnlyUsedByCodeAttribute : Attribute
    {
    }

    public class ActionGroupAttribute : Attribute
    {
        public readonly string GroupName;

        public ActionGroupAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }

    /// <summary>
    /// 支持链式方法
    /// </summary>
    public class SequenceNodeChain : ActionChain
    {
        protected override ActionKitAction mNode
        {
            get { return mSequenceNode; }
        }

        private SequenceNode mSequenceNode;

        public SequenceNodeChain()
        {
            mSequenceNode = new SequenceNode();
        }

        public override IActionChain Append(IAction node)
        {
            mSequenceNode.Append(node);
            return this;
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            mSequenceNode.Dispose();
            mSequenceNode = null;
        }
    }

    public class RepeatNodeChain : ActionChain
    {
        protected override ActionKitAction mNode
        {
            get { return mRepeatNodeAction; }
        }

        private RepeatNode mRepeatNodeAction;

        private SequenceNode mSequenceNode;

        public RepeatNodeChain(int repeatCount)
        {
            mSequenceNode = new SequenceNode();
            mRepeatNodeAction = new RepeatNode(mSequenceNode, repeatCount);
        }

        public override IActionChain Append(IAction node)
        {
            mSequenceNode.Append(node);
            return this;
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            if (null != mRepeatNodeAction)
            {
                mRepeatNodeAction.Dispose();
            }

            mRepeatNodeAction = null;

            mSequenceNode.Dispose();
            mSequenceNode = null;
        }
    }

    public interface IDisposeWhen : IDisposeEventRegister
    {
        IDisposeEventRegister DisposeWhen(Func<bool> condition);
    }

    public interface IDisposeEventRegister
    {
        void OnDisposed(System.Action onDisposedEvent);

        IDisposeEventRegister OnFinished(System.Action onFinishedEvent);
    }

    public static class IActionExtension
    {
        public static T ExecuteNode<T>(this T selBehaviour, IAction commandNode) where T : MonoBehaviour
        {
            selBehaviour.StartCoroutine(commandNode.Execute());
            return selBehaviour;
        }

        private static WaitForEndOfFrame mEndOfFrame = new WaitForEndOfFrame();

        public static IEnumerator Execute(this IAction selfNode)
        {
            if (selfNode.Finished) selfNode.Reset();

            while (!selfNode.Execute(Time.deltaTime))
            {
                yield return mEndOfFrame;
            }
        }
    }

    public static partial class IActionChainExtention
    {
        public static IActionChain Repeat<T>(this T selfbehaviour, int count = -1) where T : MonoBehaviour
        {
            var retNodeChain = new RepeatNodeChain(count) { Executer = selfbehaviour };
            retNodeChain.DisposeWhenGameObjectDestroyed(selfbehaviour);
            return retNodeChain;
        }

        public static IActionChain Sequence<T>(this T selfbehaviour) where T : MonoBehaviour
        {
            var retNodeChain = new SequenceNodeChain { Executer = selfbehaviour };
            retNodeChain.DisposeWhenGameObjectDestroyed(selfbehaviour);
            return retNodeChain;
        }

        public static IActionChain OnlyBegin(this IActionChain selfChain, Action<OnlyBeginAction> onBegin)
        {
            return selfChain.Append(OnlyBeginAction.Allocate(onBegin));
        }


        /// <summary>
        /// Same as Delayw
        /// </summary>
        /// <param name="senfChain"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static IActionChain Wait(this IActionChain senfChain, float seconds)
        {
            return senfChain.Append(DelayAction.Allocate(seconds));
        }

        public static IActionChain Event(this IActionChain selfChain, params System.Action[] onEvents)
        {
            return selfChain.Append(EventAction.Allocate(onEvents));
        }


        public static IActionChain Until(this IActionChain selfChain, Func<bool> condition)
        {
            return selfChain.Append(UntilAction.Allocate(condition));
        }
    }

    public interface IActionChain : IAction
    {
        MonoBehaviour Executer { get; set; }

        IActionChain Append(IAction node);

        IDisposeWhen Begin();
    }

    public abstract class ActionChain : ActionKitAction, IActionChain, IDisposeWhen
    {
        public MonoBehaviour Executer { get; set; }

        protected abstract ActionKitAction mNode { get; }

        public abstract IActionChain Append(IAction node);

        protected override void OnExecute(float dt)
        {
            if (mDisposeWhenCondition && mDisposeCondition != null && mDisposeCondition.Invoke())
            {
                Finish();
            }
            else
            {
                Finished = mNode.Execute(dt);
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            Dispose();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            Executer = null;
            mDisposeWhenCondition = false;
            mDisposeCondition = null;

            if (mOnDisposedEvent != null)
            {
                mOnDisposedEvent.Invoke();
            }

            mOnDisposedEvent = null;
        }

        public IDisposeWhen Begin()
        {
            Executer.ExecuteNode(this);
            return this;
        }

        private bool mDisposeWhenCondition = false;
        private Func<bool> mDisposeCondition;
        private System.Action mOnDisposedEvent = null;

        public IDisposeEventRegister DisposeWhen(Func<bool> condition)
        {
            mDisposeWhenCondition = true;
            mDisposeCondition = condition;
            return this;
        }

        IDisposeEventRegister IDisposeEventRegister.OnFinished(System.Action onFinishedEvent)
        {
            OnEndedCallback += onFinishedEvent;
            return this;
        }

        public void OnDisposed(System.Action onDisposedEvent)
        {
            mOnDisposedEvent = onDisposedEvent;
        }
    }

    /// <summary>
    /// 事件注入,和 NodeSystem 配套使用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventInjector<T>
    {
        public delegate bool InjectEventTrigger(T lastValue, T newValue);

        public delegate T InjectEventGetter();

        private T mCahedLastValue;

        public readonly Func<T> mGetter;

        public T Value
        {
            get { return mCahedLastValue; }
        }

        public EventInjector(Func<T> getter)
        {
            mGetter = getter;
        }

        public bool GetOn(InjectEventTrigger triggerConditionWithOldAndNewValue)
        {
            var value = mGetter();
            var trig = triggerConditionWithOldAndNewValue(mCahedLastValue, value);
            mCahedLastValue = value;
            return trig;
        }

        public bool GetOnValueChanged(Func<T, bool> triggerConditionWithNewValue = null)
        {
            return GetOn((lastValue, newValue) =>
                lastValue.Equals(newValue) &&
                (triggerConditionWithNewValue == null || triggerConditionWithNewValue(newValue)));
        }
    }

    [MonoSingletonPath("[ActionKit]/ActionQueue")]
    public class ActionQueue : MonoBehaviour, ISingleton
    {
        private List<IAction> mActions = new List<IAction>();

        public static void Append(IAction action)
        {
            mInstance.mActions.Add(action);
        }

        // Update is called once per frame
        private void Update()
        {
            if (mActions.Count != 0 && mActions[0].Execute(Time.deltaTime))
            {
                mActions.RemoveAt(0);
            }
        }

        void ISingleton.OnSingletonInit()
        {
        }

        private static ActionQueue mInstance
        {
            get { return MonoSingletonProperty<ActionQueue>.Instance; }
        }
    }

    #region API

    /// <summary>
    /// Unity 游戏框架搭建 (十九) 简易对象池：http://qframework.io/post/24/ 的例子
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SimpleObjectPool<T> : Pool<T>
    {
        readonly Action<T> mResetMethod;

        public SimpleObjectPool(Func<T> factoryMethod, Action<T> resetMethod = null, int initCount = 0)
        {
            mFactory = new CustomObjectFactory<T>(factoryMethod);
            mResetMethod = resetMethod;

            for (int i = 0; i < initCount; i++)
            {
                mCacheStack.Push(mFactory.Create());
            }
        }

        public override bool Recycle(T obj)
        {
            if (mResetMethod != null)
            {
                mResetMethod.Invoke(obj);
            }

            mCacheStack.Push(obj);
            return true;
        }
    }

    /// <summary>
    /// Object pool.
    /// </summary>
    internal class SafeObjectPool<T> : Pool<T>, ISingleton where T : IPoolable, new()
    {
        #region Singleton

        void ISingleton.OnSingletonInit()
        {
        }

        protected SafeObjectPool()
        {
            mFactory = new DefaultObjectFactory<T>();
        }

        public static SafeObjectPool<T> Instance
        {
            get { return SingletonProperty<SafeObjectPool<T>>.Instance; }
        }

        public void Dispose()
        {
            SingletonProperty<SafeObjectPool<T>>.Dispose();
        }

        #endregion

        /// <summary>
        /// Init the specified maxCount and initCount.
        /// </summary>
        /// <param name="maxCount">Max Cache count.</param>
        /// <param name="initCount">Init Cache count.</param>
        public void Init(int maxCount, int initCount)
        {
            MaxCacheCount = maxCount;

            if (maxCount > 0)
            {
                initCount = Math.Min(maxCount, initCount);
            }

            if (CurCount < initCount)
            {
                for (var i = CurCount; i < initCount; ++i)
                {
                    Recycle(new T());
                }
            }
        }

        /// <summary>
        /// Gets or sets the max cache count.
        /// </summary>
        /// <value>The max cache count.</value>
        public int MaxCacheCount
        {
            get { return mMaxCount; }
            set
            {
                mMaxCount = value;

                if (mCacheStack != null)
                {
                    if (mMaxCount > 0)
                    {
                        if (mMaxCount < mCacheStack.Count)
                        {
                            int removeCount = mCacheStack.Count - mMaxCount;
                            while (removeCount > 0)
                            {
                                mCacheStack.Pop();
                                --removeCount;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Allocate T instance.
        /// </summary>
        public override T Allocate()
        {
            var result = base.Allocate();
            result.IsRecycled = false;
            return result;
        }

        /// <summary>
        /// Recycle the T instance
        /// </summary>
        /// <param name="t">T.</param>
        public override bool Recycle(T t)
        {
            if (t == null || t.IsRecycled)
            {
                return false;
            }

            if (mMaxCount > 0)
            {
                if (mCacheStack.Count >= mMaxCount)
                {
                    t.OnRecycled();
                    return false;
                }
            }

            t.IsRecycled = true;
            t.OnRecycled();
            mCacheStack.Push(t);

            return true;
        }
    }

    /// <summary>
    /// Object pool 4 class who no public constructor
    /// such as SingletonClass.QEventSystem
    /// </summary>
    internal class NonPublicObjectPool<T> : Pool<T>, ISingleton where T : class, IPoolable
    {
        #region Singleton

        public void OnSingletonInit()
        {
        }

        public static NonPublicObjectPool<T> Instance
        {
            get { return SingletonProperty<NonPublicObjectPool<T>>.Instance; }
        }

        protected NonPublicObjectPool()
        {
            mFactory = new NonPublicObjectFactory<T>();
        }

        public void Dispose()
        {
            SingletonProperty<NonPublicObjectPool<T>>.Dispose();
        }

        #endregion

        /// <summary>
        /// Init the specified maxCount and initCount.
        /// </summary>
        /// <param name="maxCount">Max Cache count.</param>
        /// <param name="initCount">Init Cache count.</param>
        public void Init(int maxCount, int initCount)
        {
            if (maxCount > 0)
            {
                initCount = Math.Min(maxCount, initCount);
            }

            if (CurCount >= initCount) return;

            for (var i = CurCount; i < initCount; ++i)
            {
                Recycle(mFactory.Create());
            }
        }

        /// <summary>
        /// Gets or sets the max cache count.
        /// </summary>
        /// <value>The max cache count.</value>
        public int MaxCacheCount
        {
            get { return mMaxCount; }
            set
            {
                mMaxCount = value;

                if (mCacheStack == null) return;
                if (mMaxCount <= 0) return;
                if (mMaxCount >= mCacheStack.Count) return;
                var removeCount = mMaxCount - mCacheStack.Count;
                while (removeCount > 0)
                {
                    mCacheStack.Pop();
                    --removeCount;
                }
            }
        }

        /// <summary>
        /// Allocate T instance.
        /// </summary>
        public override T Allocate()
        {
            var result = base.Allocate();
            result.IsRecycled = false;
            return result;
        }

        /// <summary>
        /// Recycle the T instance
        /// </summary>
        /// <param name="t">T.</param>
        public override bool Recycle(T t)
        {
            if (t == null || t.IsRecycled)
            {
                return false;
            }

            if (mMaxCount > 0)
            {
                if (mCacheStack.Count >= mMaxCount)
                {
                    t.OnRecycled();
                    return false;
                }
            }

            t.IsRecycled = true;
            t.OnRecycled();
            mCacheStack.Push(t);

            return true;
        }
    }

    internal abstract class AbstractPool<T> where T : AbstractPool<T>, new()
    {
        private static Stack<T> mPool = new Stack<T>(10);

        protected bool mInPool = false;

        public static T Allocate()
        {
            var node = mPool.Count == 0 ? new T() : mPool.Pop();
            node.mInPool = false;
            return node;
        }

        public void Recycle2Cache()
        {
            OnRecycle();
            mInPool = true;
            mPool.Push(this as T);
        }

        protected abstract void OnRecycle();
    }

    #endregion

    #region interfaces

    /// <summary>
    /// 对象池接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IPool<T>
    {
        /// <summary>
        /// 分配对象
        /// </summary>
        /// <returns></returns>
        T Allocate();

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool Recycle(T obj);
    }

    /// <summary>
    /// I pool able.
    /// </summary>
    internal interface IPoolable
    {
        void OnRecycled();
        bool IsRecycled { get; set; }
    }

    /// <summary>
    /// I cache type.
    /// </summary>
    internal interface IPoolType
    {
        void Recycle2Cache();
    }

    /// <summary>
    /// 对象池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class Pool<T> : IPool<T>
    {
        #region ICountObserverable

        /// <summary>
        /// Gets the current count.
        /// </summary>
        /// <value>The current count.</value>
        public int CurCount
        {
            get { return mCacheStack.Count; }
        }

        #endregion

        protected IObjectFactory<T> mFactory;

        /// <summary>
        /// 存储相关数据的栈
        /// </summary>
        protected readonly Stack<T> mCacheStack = new Stack<T>();

        /// <summary>
        /// default is 5
        /// </summary>
        protected int mMaxCount = 12;

        public virtual T Allocate()
        {
            return mCacheStack.Count == 0
                ? mFactory.Create()
                : mCacheStack.Pop();
        }

        public abstract bool Recycle(T obj);
    }

    #endregion

    #region DataStructurePool

    /// <summary>
    /// 字典对象池：用于存储相关对象
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    internal class DictionaryPool<TKey, TValue>
    {
        /// <summary>
        /// 栈对象：存储多个字典
        /// </summary>
        static Stack<Dictionary<TKey, TValue>> mListStack = new Stack<Dictionary<TKey, TValue>>(8);

        /// <summary>
        /// 出栈：从栈中获取某个字典数据
        /// </summary>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> Get()
        {
            if (mListStack.Count == 0)
            {
                return new Dictionary<TKey, TValue>(8);
            }

            return mListStack.Pop();
        }

        /// <summary>
        /// 入栈：将字典数据存储到栈中 
        /// </summary>
        /// <param name="toRelease"></param>
        public static void Release(Dictionary<TKey, TValue> toRelease)
        {
            toRelease.Clear();
            mListStack.Push(toRelease);
        }
    }

    /// <summary>
    /// 对象池字典 拓展方法类
    /// </summary>
    internal static class DictionaryPoolExtensions
    {
        /// <summary>
        /// 对字典拓展 自身入栈 的方法
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="toRelease"></param>
        public static void Release2Pool<TKey, TValue>(this Dictionary<TKey, TValue> toRelease)
        {
            DictionaryPool<TKey, TValue>.Release(toRelease);
        }
    }

    /// <summary>
    /// 链表对象池：存储相关对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class ListPool<T>
    {
        /// <summary>
        /// 栈对象：存储多个List
        /// </summary>
        static Stack<List<T>> mListStack = new Stack<List<T>>(8);

        /// <summary>
        /// 出栈：获取某个List对象
        /// </summary>
        /// <returns></returns>
        public static List<T> Get()
        {
            if (mListStack.Count == 0)
            {
                return new List<T>(8);
            }

            return mListStack.Pop();
        }

        /// <summary>
        /// 入栈：将List对象添加到栈中
        /// </summary>
        /// <param name="toRelease"></param>
        public static void Release(List<T> toRelease)
        {
            toRelease.Clear();
            mListStack.Push(toRelease);
        }
    }

    /// <summary>
    /// 链表对象池 拓展方法类
    /// </summary>
    internal static class ListPoolExtensions
    {
        /// <summary>
        /// 给List拓展 自身入栈 的方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toRelease"></param>
        public static void Release2Pool<T>(this List<T> toRelease)
        {
            ListPool<T>.Release(toRelease);
        }
    }

    #endregion

    #region Factories

    /// <summary>
    /// 对象工厂
    /// </summary>
    internal class ObjectFactory
    {
        /// <summary>
        /// 动态创建类的实例：创建有参的构造函数
        /// </summary>
        /// <param name="type"></param>
        /// <param name="constructorArgs"></param>
        /// <returns></returns>
        public static object Create(Type type, params object[] constructorArgs)
        {
            return Activator.CreateInstance(type, constructorArgs);
        }

        /// <summary>
        /// 动态创建类的实例：泛型扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructorArgs"></param>
        /// <returns></returns>
        public static T Create<T>(params object[] constructorArgs)
        {
            return (T)Create(typeof(T), constructorArgs);
        }

        /// <summary>
        /// 动态创建类的实例：创建无参/私有的构造函数
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object CreateNonPublicConstructorObject(Type type)
        {
            // 获取私有构造函数
            var constructorInfos = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

            // 获取无参构造函数
            var ctor = Array.Find(constructorInfos, c => c.GetParameters().Length == 0);

            if (ctor == null)
            {
                throw new Exception("Non-Public Constructor() not found! in " + type);
            }

            return ctor.Invoke(null);
        }

        /// <summary>
        /// 动态创建类的实例：创建无参/私有的构造函数  泛型扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateNonPublicConstructorObject<T>()
        {
            return (T)CreateNonPublicConstructorObject(typeof(T));
        }

        /// <summary>
        /// 创建带有初始化回调的 对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="onObjectCreate"></param>
        /// <param name="constructorArgs"></param>
        /// <returns></returns>
        public static object CreateWithInitialAction(Type type, Action<object> onObjectCreate,
            params object[] constructorArgs)
        {
            var obj = Create(type, constructorArgs);
            onObjectCreate(obj);
            return obj;
        }

        /// <summary>
        /// 创建带有初始化回调的 对象：泛型扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="onObjectCreate"></param>
        /// <param name="constructorArgs"></param>
        /// <returns></returns>
        public static T CreateWithInitialAction<T>(Action<T> onObjectCreate,
            params object[] constructorArgs)
        {
            var obj = Create<T>(constructorArgs);
            onObjectCreate(obj);
            return obj;
        }
    }

    /// <summary>
    /// 自定义对象工厂：相关对象是 自己定义 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class CustomObjectFactory<T> : IObjectFactory<T>
    {
        public CustomObjectFactory(Func<T> factoryMethod)
        {
            mFactoryMethod = factoryMethod;
        }

        protected Func<T> mFactoryMethod;

        public T Create()
        {
            return mFactoryMethod();
        }
    }

    /// <summary>
    /// 默认对象工厂：相关对象是通过New 出来的
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DefaultObjectFactory<T> : IObjectFactory<T> where T : new()
    {
        public T Create()
        {
            return new T();
        }
    }

    /// <summary>
    /// 对象工厂接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IObjectFactory<T>
    {
        /// <summary>
        /// 创建对象
        /// </summary>
        /// <returns></returns>
        T Create();
    }

    /// <summary>
    /// 没有公共构造函数的对象工厂：相关对象只能通过反射获得
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class NonPublicObjectFactory<T> : IObjectFactory<T> where T : class
    {
        public T Create()
        {
            var ctors = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            var ctor = Array.Find(ctors, c => c.GetParameters().Length == 0);

            if (ctor == null)
            {
                throw new Exception("Non-Public Constructor() not found! in " + typeof(T) + "\n 在没有找到非 public 的构造方法");
            }

            return ctor.Invoke(null) as T;
        }
    }

    #endregion


    #region SingletonKit For Pool

    /// <summary>
    /// 单例接口
    /// </summary>
    internal interface ISingleton
    {
        /// <summary>
        /// 单例初始化(继承当前接口的类都需要实现该方法)
        /// </summary>
        void OnSingletonInit();
    }

    /// <summary>
    /// 普通类的单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class Singleton<T> : ISingleton where T : Singleton<T>
    {
        /// <summary>
        /// 静态实例
        /// </summary>
        protected static T mInstance;

        /// <summary>
        /// 标签锁：确保当一个线程位于代码的临界区时，另一个线程不进入临界区。
        /// 如果其他线程试图进入锁定的代码，则它将一直等待（即被阻止），直到该对象被释放
        /// </summary>
        static object mLock = new object();

        /// <summary>
        /// 静态属性
        /// </summary>
        public static T Instance
        {
            get
            {
                lock (mLock)
                {
                    if (mInstance == null)
                    {
                        mInstance = SingletonCreator.CreateSingleton<T>();
                    }
                }

                return mInstance;
            }
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public virtual void Dispose()
        {
            mInstance = null;
        }

        /// <summary>
        /// 单例初始化方法
        /// </summary>
        public virtual void OnSingletonInit()
        {
        }
    }

    /// <summary>
    /// 属性单例类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class SingletonProperty<T> where T : class, ISingleton
    {
        /// <summary>
        /// 静态实例
        /// </summary>
        private static T mInstance;

        /// <summary>
        /// 标签锁
        /// </summary>
        private static readonly object mLock = new object();

        /// <summary>
        /// 静态属性
        /// </summary>
        public static T Instance
        {
            get
            {
                lock (mLock)
                {
                    if (mInstance == null)
                    {
                        mInstance = SingletonCreator.CreateSingleton<T>();
                    }
                }

                return mInstance;
            }
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public static void Dispose()
        {
            mInstance = null;
        }
    }

    /// <summary>
    /// 普通单例创建类
    /// </summary>
    internal static class SingletonCreator
    {
        static T CreateNonPublicConstructorObject<T>() where T : class
        {
            var type = typeof(T);
            // 获取私有构造函数
            var constructorInfos = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

            // 获取无参构造函数
            var ctor = Array.Find(constructorInfos, c => c.GetParameters().Length == 0);

            if (ctor == null)
            {
                throw new Exception("Non-Public Constructor() not found! in " + type);
            }

            return ctor.Invoke(null) as T;
        }

        public static T CreateSingleton<T>() where T : class, ISingleton
        {
            var type = typeof(T);
            var monoBehaviourType = typeof(MonoBehaviour);

            if (monoBehaviourType.IsAssignableFrom(type))
            {
                return CreateMonoSingleton<T>();
            }
            else
            {
                var instance = CreateNonPublicConstructorObject<T>();
                instance.OnSingletonInit();
                return instance;
            }
        }


        /// <summary>
        /// 单元测试模式 标签
        /// </summary>
        public static bool IsUnitTestMode { get; set; }

        /// <summary>
        /// 查找Obj（一个嵌套查找Obj的过程）
        /// </summary>
        /// <param name="root">父节点</param>
        /// <param name="subPath">拆分后的路径节点</param>
        /// <param name="index">下标</param>
        /// <param name="build">true</param>
        /// <param name="dontDestroy">不要销毁 标签</param>
        /// <returns></returns>
        private static GameObject FindGameObject(GameObject root, string[] subPath, int index, bool build,
            bool dontDestroy)
        {
            GameObject client = null;

            if (root == null)
            {
                client = GameObject.Find(subPath[index]);
            }
            else
            {
                var child = root.transform.Find(subPath[index]);
                if (child != null)
                {
                    client = child.gameObject;
                }
            }

            if (client == null)
            {
                if (build)
                {
                    client = new GameObject(subPath[index]);
                    if (root != null)
                    {
                        client.transform.SetParent(root.transform);
                    }

                    if (dontDestroy && index == 0 && !IsUnitTestMode)
                    {
                        GameObject.DontDestroyOnLoad(client);
                    }
                }
            }

            if (client == null)
            {
                return null;
            }

            return ++index == subPath.Length ? client : FindGameObject(client, subPath, index, build, dontDestroy);
        }

        /// <summary>
        /// 泛型方法：创建MonoBehaviour单例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateMonoSingleton<T>() where T : class, ISingleton
        {
            T instance = null;
            var type = typeof(T);

            //判断T实例存在的条件是否满足
            if (!IsUnitTestMode && !Application.isPlaying)
                return instance;

            //判断当前场景中是否存在T实例
            instance = UnityEngine.Object.FindObjectOfType(type) as T;
            if (instance != null)
            {
                instance.OnSingletonInit();
                return instance;
            }

            //MemberInfo：获取有关成员属性的信息并提供对成员元数据的访问
            MemberInfo info = typeof(T);
            //获取T类型 自定义属性，并找到相关路径属性，利用该属性创建T实例
            var attributes = info.GetCustomAttributes(true);
            foreach (var atribute in attributes)
            {
                var defineAttri = atribute as MonoSingletonPath;
                if (defineAttri == null)
                {
                    continue;
                }

                instance = CreateComponentOnGameObject<T>(defineAttri.PathInHierarchy, true);
                break;
            }

            //如果还是无法找到instance  则主动去创建同名Obj 并挂载相关脚本 组件
            if (instance == null)
            {
                var obj = new GameObject(typeof(T).Name);
                if (!IsUnitTestMode)
                    UnityEngine.Object.DontDestroyOnLoad(obj);
                instance = obj.AddComponent(typeof(T)) as T;
            }

            instance.OnSingletonInit();
            return instance;
        }

        /// <summary>
        /// 在GameObject上创建T组件（脚本）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">路径（应该就是Hierarchy下的树结构路径）</param>
        /// <param name="dontDestroy">不要销毁 标签</param>
        /// <returns></returns>
        private static T CreateComponentOnGameObject<T>(string path, bool dontDestroy) where T : class
        {
            var obj = FindGameObject(path, true, dontDestroy);
            if (obj == null)
            {
                obj = new GameObject("Singleton of " + typeof(T).Name);
                if (dontDestroy && !IsUnitTestMode)
                {
                    UnityEngine.Object.DontDestroyOnLoad(obj);
                }
            }

            return obj.AddComponent(typeof(T)) as T;
        }

        /// <summary>
        /// 查找Obj（对于路径 进行拆分）
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="build">true</param>
        /// <param name="dontDestroy">不要销毁 标签</param>
        /// <returns></returns>
        private static GameObject FindGameObject(string path, bool build, bool dontDestroy)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var subPath = path.Split('/');
            if (subPath == null || subPath.Length == 0)
            {
                return null;
            }

            return FindGameObject(null, subPath, 0, build, dontDestroy);
        }
    }

    /// <summary>
    /// 静态类：MonoBehaviour类的单例
    /// 泛型类：Where约束表示T类型必须继承MonoSingleton<T>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class MonoSingleton<T> : MonoBehaviour, ISingleton where T : MonoSingleton<T>
    {
        /// <summary>
        /// 静态实例
        /// </summary>
        protected static T mInstance;

        /// <summary>
        /// 静态属性：封装相关实例对象
        /// </summary>
        public static T Instance
        {
            get
            {
                if (mInstance == null && !mOnApplicationQuit)
                {
                    mInstance = SingletonCreator.CreateMonoSingleton<T>();
                }

                return mInstance;
            }
        }

        /// <summary>
        /// 实现接口的单例初始化
        /// </summary>
        public virtual void OnSingletonInit()
        {
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public virtual void Dispose()
        {
            if (SingletonCreator.IsUnitTestMode)
            {
                var curTrans = transform;
                do
                {
                    var parent = curTrans.parent;
                    DestroyImmediate(curTrans.gameObject);
                    curTrans = parent;
                } while (curTrans != null);

                mInstance = null;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 当前应用程序是否结束 标签
        /// </summary>
        protected static bool mOnApplicationQuit = false;

        /// <summary>
        /// 应用程序退出：释放当前对象并销毁相关GameObject
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            mOnApplicationQuit = true;
            if (mInstance == null) return;
            Destroy(mInstance.gameObject);
            mInstance = null;
        }

        /// <summary>
        /// 释放当前对象
        /// </summary>
        protected virtual void OnDestroy()
        {
            mInstance = null;
        }

        /// <summary>
        /// 判断当前应用程序是否退出
        /// </summary>
        public static bool IsApplicationQuit
        {
            get { return mOnApplicationQuit; }
        }
    }

    /// <summary>
    /// MonoSingleton路径
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)] //这个特性只能标记在Class上
    internal class MonoSingletonPath : Attribute
    {
        private string mPathInHierarchy;

        public MonoSingletonPath(string pathInHierarchy)
        {
            mPathInHierarchy = pathInHierarchy;
        }

        public string PathInHierarchy
        {
            get { return mPathInHierarchy; }
        }
    }

    /// <summary>
    /// 继承Mono的属性单例？
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class MonoSingletonProperty<T> where T : MonoBehaviour, ISingleton
    {
        private static T mInstance;

        public static T Instance
        {
            get
            {
                if (null == mInstance)
                {
                    mInstance = SingletonCreator.CreateMonoSingleton<T>();
                }

                return mInstance;
            }
        }

        public static void Dispose()
        {
            if (SingletonCreator.IsUnitTestMode)
            {
                UnityEngine.Object.DestroyImmediate(mInstance.gameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(mInstance.gameObject);
            }

            mInstance = null;
        }
    }

    /// <summary>
    /// 如果跳转到新的场景里已经有了实例，则不创建新的单例（或者创建新的单例后会销毁掉新的单例）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class PersistentMonoSingleton<T> : MonoBehaviour where T : Component
    {
        protected static T mInstance;
        protected bool mEnabled;

        /// <summary>
        /// Singleton design pattern
        /// </summary>
        /// <value>The instance.</value>
        public static T Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = FindObjectOfType<T>();
                    if (mInstance == null)
                    {
                        var obj = new GameObject();
                        mInstance = obj.AddComponent<T>();
                    }
                }

                return mInstance;
            }
        }

        /// <summary>
        /// On awake, we check if there's already a copy of the object in the scene. If there's one, we destroy it.
        /// </summary>
        protected virtual void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (mInstance == null)
            {
                //If I am the first instance, make me the Singleton
                mInstance = this as T;
                DontDestroyOnLoad(transform.gameObject);
                mEnabled = true;
            }
            else
            {
                //If a Singleton already exists and you find
                //another reference in scene, destroy it!
                if (this != mInstance)
                {
                    Destroy(this.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 如果跳转到新的场景里已经有了实例，则删除已有示例，再创建新的实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ReplaceableMonoSingleton<T> : MonoBehaviour where T : Component
    {
        protected static T mInstance;
        public float InitializationTime;

        /// <summary>
        /// Singleton design pattern
        /// </summary>
        /// <value>The instance.</value>
        public static T Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = FindObjectOfType<T>();
                    if (mInstance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.hideFlags = HideFlags.HideAndDontSave;
                        mInstance = obj.AddComponent<T>();
                    }
                }

                return mInstance;
            }
        }

        /// <summary>
        /// On awake, we check if there's already a copy of the object in the scene. If there's one, we destroy it.
        /// </summary>
        protected virtual void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            InitializationTime = Time.time;

            DontDestroyOnLoad(this.gameObject);
            // we check for existing objects of the same type
            T[] check = FindObjectsOfType<T>();
            foreach (T searched in check)
            {
                if (searched != this)
                {
                    // if we find another object of the same type (not this), and if it's older than our current object, we destroy it.
                    if (searched.GetComponent<ReplaceableMonoSingleton<T>>().InitializationTime < InitializationTime)
                    {
                        Destroy(searched.gameObject);
                    }
                }
            }

            if (mInstance == null)
            {
                mInstance = this as T;
            }
        }
    }

    #endregion

    public abstract class ActionKitTable<TDataItem> : IEnumerable<TDataItem>, IDisposable
    {
        public void Add(TDataItem item)
        {
            OnAdd(item);
        }

        public void Remove(TDataItem item)
        {
            OnRemove(item);
        }

        public void Clear()
        {
            OnClear();
        }

        // 改，由于 TDataItem 是引用类型，所以直接改值即可。
        public void Update()
        {
        }

        protected abstract void OnAdd(TDataItem item);
        protected abstract void OnRemove(TDataItem item);

        protected abstract void OnClear();


        public abstract IEnumerator<TDataItem> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            OnDispose();
        }

        protected abstract void OnDispose();
    }

    public class ActionKitTableIndex<TKeyType, TDataItem> : IDisposable
    {
        private Dictionary<TKeyType, List<TDataItem>> mIndex = new Dictionary<TKeyType, List<TDataItem>>();

        private Func<TDataItem, TKeyType> mGetKeyByDataItem = null;

        public ActionKitTableIndex(Func<TDataItem, TKeyType> keyGetter)
        {
            mGetKeyByDataItem = keyGetter;
        }

        public IDictionary<TKeyType, List<TDataItem>> Dictionary
        {
            get { return mIndex; }
        }

        public void Add(TDataItem dataItem)
        {
            var key = mGetKeyByDataItem(dataItem);

            if (mIndex.ContainsKey(key))
            {
                mIndex[key].Add(dataItem);
            }
            else
            {
                var list = ListPool<TDataItem>.Get();

                list.Add(dataItem);

                mIndex.Add(key, list);
            }
        }

        public void Remove(TDataItem dataItem)
        {
            var key = mGetKeyByDataItem(dataItem);

            mIndex[key].Remove(dataItem);
        }

        public IEnumerable<TDataItem> Get(TKeyType key)
        {
            List<TDataItem> retList = null;

            if (mIndex.TryGetValue(key, out retList))
            {
                return retList;
            }

            // 返回一个空的集合
            return Enumerable.Empty<TDataItem>();
        }

        public void Clear()
        {
            foreach (var value in mIndex.Values)
            {
                value.Clear();
            }

            mIndex.Clear();
        }


        public void Dispose()
        {
            foreach (var value in mIndex.Values)
            {
                value.Release2Pool();
            }

            mIndex.Release2Pool();

            mIndex = null;
        }
    }


    #region ECA

    public class EmptyEventData
    {
        public static EmptyEventData Default;
    }

    public interface IECAEvent<T>
    {
        void Register(UnityAction<T> onEvent);
        void UnRegister(UnityAction<T> onEvent);
        void Trigger(T t);
    }

    public interface IECACondition<T>
    {
        bool Match(T t);
    }

    public interface IECAAction<T>
    {
        void Execute(T t);
    }

    public interface IECARule<T> : IDisposable
    {
        IECAEvent<T> E { get; set; }
        IECACondition<T> C { get; set; }
        IECAAction<T> A { get; set; }

        IECARule<T> Build();
    }

    public class ECARule<T> : IECARule<T>
    {
        public IECAEvent<T> E { get; set; }
        public IECACondition<T> C { get; set; }
        public IECAAction<T> A { get; set; }


        void OnEvent(T t)
        {
            if (C.Match(t))
            {
                A.Execute(t);
            }
        }

        public IECARule<T> Build()
        {
            E.Register(OnEvent);
            return this;
        }

        public void Dispose()
        {
            E.UnRegister(OnEvent);
            E = null;
            C = null;
            A = null;
        }
    }

    public class CustomECACondition<T> : IECACondition<T>
    {
        private readonly Func<T, bool> mCondition;

        public CustomECACondition(Func<T, bool> condition)
        {
            mCondition = condition;
        }


        public bool Match(T t)
        {
            return mCondition.Invoke(t);
        }
    }

    public class CustomECAAction<T> : IECAAction<T>
    {
        private readonly UnityAction<T> mAction;

        public CustomECAAction(UnityAction<T> action)
        {
            mAction = action;
        }


        public void Execute(T t)
        {
            mAction?.Invoke(t);
        }
    }

    public static class ECARuleExtension
    {
        public static IECARule<T> Condition<T>(this IECARule<T> self, Func<T, bool> condition)
        {
            self.C = new CustomECACondition<T>(condition);
            return self;
        }

        public static IECARule<T> Condition<T>(this IECARule<T> self, Func<bool> condition)
        {
            self.C = new CustomECACondition<T>(_ => condition());
            return self;
        }

        public static IECARule<T> Action<T>(this IECARule<T> self, UnityAction<T> action)
        {
            self.A = new CustomECAAction<T>(action);
            return self;
        }

        public static IECARule<T> Action<T>(this IECARule<T> self, UnityAction action)
        {
            self.A = new CustomECAAction<T>(_ => action());
            return self;
        }
    }

    public class UnityEventT<T> : UnityEvent<T>
    {
    }

    public class MonoEventTrigger<T> : MonoBehaviour, IECAEvent<T>
    {
        public UnityEvent<T> Event = new UnityEventT<T>();

        public void Register(UnityAction<T> onEvent)
        {
            Event.AddListener(onEvent);
        }

        public void UnRegister(UnityAction<T> onEvent)
        {
            Event.RemoveListener(onEvent);
        }

        public void Trigger(T t)
        {
            Event?.Invoke(t);
        }
    }


    public class UpdateTrigger : MonoEventTrigger<EmptyEventData>
    {
        private void Update()
        {
            Trigger(EmptyEventData.Default);
        }
    }

    public class FixedUpdateTrigger : MonoEventTrigger<EmptyEventData>
    {
        private void FixedUpdate()
        {
            Trigger(EmptyEventData.Default);
        }
    }

    public class LateUpdateTrigger : MonoEventTrigger<EmptyEventData>
    {
        private void LateUpdate()
        {
            Trigger(EmptyEventData.Default);
        }
    }

    public class OnTriggerEnter2DTrigger : MonoEventTrigger<Collider2D>
    {
        private void OnTriggerEnter2D(Collider2D col)
        {
            Trigger(col);
        }
    }

    public static class ECARuleTriggerExtension
    {
        public static void OnUpdate(this Component self, UnityAction onUpdate)
        {
            self.UpdateEvent().Condition(_ => true).Action(_ => onUpdate()).Build();
        }

        public static void OnFixedUpdate(this Component self, UnityAction onFixedUpdate)
        {
            self.FixedUpdateEvent().Condition(_ => true).Action(_ => onFixedUpdate()).Build();
        }

        public static void OnLateUpdate(this Component self, UnityAction onLateUpdate)
        {
            self.LateUpdateEvent().Condition(_ => true).Action(_ => onLateUpdate()).Build();
        }

        public static IECARule<EmptyEventData> UpdateEvent(this Component self)
        {
            return new ECARule<EmptyEventData>
            {
                E = Utils.GetOrAddComponent<UpdateTrigger>(self.gameObject)
            };
        }

        public static IECARule<EmptyEventData> FixedUpdateEvent(this Component self)
        {
            return new ECARule<EmptyEventData>
            {
                E = Utils.GetOrAddComponent<FixedUpdateTrigger>(self.gameObject)
            };
        }

        public static IECARule<EmptyEventData> LateUpdateEvent(this Component self)
        {
            return new ECARule<EmptyEventData>
            {
                E = Utils.GetOrAddComponent<LateUpdateTrigger>(self.gameObject)
            };
        }
    }

    internal static class Utils
    {
        public static T GetOrAddComponent<T>(GameObject gameObject) where T : MonoBehaviour
        {
            var t = gameObject.GetComponent<T>();

            if (!t)
            {
                t = gameObject.AddComponent<T>();
            }

            return t;
        }
    }

    #endregion
}