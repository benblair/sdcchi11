using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bbr.Diagnostics;
using Bbr.Extensions;
using Bbr.Zaphod;

namespace Cerrio.Samples.SDC
{
    class ReLayoutQueuer
    {
        private Dictionary<string, List<InputData>> m_data = new Dictionary<string, List<InputData>>();
        private bool m_loaded;

        private Dictionary<string, State> m_states = new Dictionary<string, State>();
        private object m_lockObject = new object();
        private Dictionary<string, PerUserGrouper> m_groupers = new Dictionary<string, PerUserGrouper>();
        private Pig<OutputData, string> m_outputPig;

        public void AddOuputHog(Pig<OutputData,string> outputPig)
        {
            m_outputPig = outputPig;

            foreach(string user in m_data.Keys)
            {
                bool start = m_loaded && !m_groupers.ContainsKey(user);

                m_groupers[user] = new PerUserGrouper(outputPig,user);

                if (start)
                {
                    StartRelayout(user);
                }
            }
        }

        public void Add(InputData data)
        {
            if(!m_data.ContainsKey(data.RequestingUser))
            {
                m_data[data.RequestingUser] = new List<InputData>();
            }

            if(!m_groupers.ContainsKey(data.RequestingUser) && null!=m_outputPig)
            {
                m_groupers[data.RequestingUser] = new PerUserGrouper(m_outputPig,data.RequestingUser);
            }

            lock (m_lockObject)
            {
                m_data[data.RequestingUser].Add(data);
            }

            if(m_loaded)
            {
                StartRelayout(data.RequestingUser);
            }
        }

        public void Modify(InputData data)
        {
            if (m_loaded)
            {
                StartRelayout(data.RequestingUser);
            }
        }

        public void Delete(InputData data)
        {
            if(m_data.ContainsKey(data.RequestingUser))
            {
                lock (m_lockObject)
                {
                    m_data[data.RequestingUser].Remove(data);
                }
            }
        }

        private void StartRelayout(string user)
        {
            lock(m_lockObject)
            {
                bool start = false;

                if(!m_states.ContainsKey(user)||m_states[user]==State.Good)
                {
                    start = m_groupers.ContainsKey(user);
                }

                m_states[user] = State.Dirty;

                if(start)
                {
                    ThreadPool.QueueUserWorkItem(Relayout, user);
                }
            }
        }

        private void Relayout(object o)
        {
            try
            {
                string user = (String)o;

                lock (m_lockObject)
                {
                    m_states[user] = State.Running;
                }

                EventLog.Log("relaying out: "+user);
                List<InputData> data;

                lock(m_lockObject)
                {
                    data = m_data[user].ToList();
                }

                m_groupers[user].DoAnalysis(data);

                bool start = false;
                lock(m_lockObject)
                {
                    if(m_states[user]==State.Running)
                    {
                        m_states[user] = State.Good;
                    }
                    else
                    {
                        start = true;
                    }
                }

                if(start)
                {
                    ThreadPool.QueueUserWorkItem(Relayout, user);
                }
            }
            catch (Exception ex)
            {
                EventLog.Log(ProgramState.RecoverableError,"Couldn't relayout a user: "+ex);
            }
        }

        private enum State
        {
            Good=0,
            Running,
            Dirty,
        }

        public void Loaded()
        {
            m_loaded = true;
            m_data.Keys.ForEach(StartRelayout);
        }
    }
}
