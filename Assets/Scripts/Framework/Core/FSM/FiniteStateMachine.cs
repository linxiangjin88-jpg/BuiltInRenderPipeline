using System;
using System.Collections.Generic;
using Scripts.Framework.Core.Log;

namespace Scripts.Framework.Core.FSM
{
    public class FiniteStateMachine<TOwner> : IDisposer
    {
        private TOwner m_owner;
        private IFsmState<TOwner> m_currentState;
        private readonly Dictionary<Type, IFsmState<TOwner>> m_states;
        private bool m_isChangingState;
        private int m_changeStateDepth;
        private const int MaxChangeStateDepth = 8;
        
        public TOwner Owner => m_owner;
        public IFsmState<TOwner> CurrentState => m_currentState;

        public FiniteStateMachine(TOwner owner)
        {
            m_owner = owner;
            IsDisposed = false;
            m_currentState = null;
            m_states = new Dictionary<Type, IFsmState<TOwner>>();
        }
        
        public void AddState<TState>(TState state) where TState : IFsmState<TOwner>
        {
            if (IsDisposed)
            {
                return;
            }

            if (state == null)
            {
                return;
            }
            
            Type stateType = typeof(TState);
            if (m_states.TryGetValue(stateType, out _))
            {
                GameLog.Warning($"State {stateType.Name} already exists in the FSM.", "FSM");
                return;
            }
            
            m_states.Add(stateType, state);
        }

        public void ChangeState<TState>(bool bForce = false) where TState : IFsmState<TOwner>, new()
        {
            if (IsDisposed)
            {
                return;
            }

            if (m_isChangingState)
            {
                GameLog.Warning($"Already changing state, can't change to {typeof(TState).Name} now.", "FSM");
                return;
            }

            if (m_changeStateDepth >= MaxChangeStateDepth)
            {
                GameLog.Error($"ChangeState depth exceeded {MaxChangeStateDepth}, possible infinite recursion! Trying to enter {typeof(TState).Name}.", tag: "FSM");
                return;
            }
            
            Type stateType = typeof(TState);
            if (!bForce && m_currentState != null && stateType == m_currentState.GetType())
            {
                return;
            }
            
            if (!m_states.TryGetValue(stateType, out var nextState))
            {
                nextState = new TState();
                m_states.Add(stateType, nextState);
            }

            m_isChangingState = true;
            try
            {
                if (m_currentState != null)
                {
                    m_currentState.OnExit(m_owner);
                }
            }
            catch (Exception e)
            {
                GameLog.Fatal($"Exception in OnExit of {m_currentState?.GetType().Name}.", e, "FSM");
            }
            finally
            {
                m_isChangingState = false;
            }

            // OnExit 期间外部可能调用了 Dispose，需中止切换
            if (IsDisposed)
            {
                return;
            }
            
            m_currentState = nextState;
            m_changeStateDepth++;
            try
            {
                m_currentState.OnEnter(m_owner);
            }
            catch (Exception e)
            {
                GameLog.Fatal($"Exception in OnEnter of {m_currentState?.GetType().Name}.", e, "FSM");
            }
            finally
            {
                m_changeStateDepth--;
            }
        }

        public void Update(float deltaTime)
        {
            if (IsDisposed)
            {
                return;
            }
            
            m_currentState?.OnUpdate(m_owner, deltaTime);
        }
        
        public bool IsDisposed { get; private set; }
        public bool Dispose()
        {
            if (IsDisposed)
            {
                return false;
            }

            try
            {
                m_currentState?.OnExit(m_owner);
            }
            catch (Exception e)
            {
                GameLog.Fatal($"Exception in OnExit of {m_currentState?.GetType().Name} during Dispose.", e, "FSM");
            }
            finally
            {
                foreach (var state in m_states.Values)
                {
                    // 如果有继承IDispose这个，就需要释放
                    if (state is IDisposer disposableState && !disposableState.IsDisposed)
                    {
                        disposableState.Dispose();
                    }
                }
                m_states.Clear();
                m_currentState = null;
                IsDisposed = true;
                m_owner = default;
            }
            return true;
        }
    }
}