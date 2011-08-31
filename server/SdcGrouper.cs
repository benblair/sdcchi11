using System;
using Bbr.Diagnostics;
using Bbr.Pigskin.Interfaces;
using Bbr.Zaphod;

namespace Cerrio.Samples.SDC
{
    class SdcGrouper : ISample, IPigHandler
    {
        private IPigPen m_pigPen;
        private Pig<InputData, string> m_inputPig;
        private Pig<OutputData, string> m_ouptutPig;
        private ReLayoutQueuer m_reLayoutQueuer = new ReLayoutQueuer();

        private string m_inputUri = "DisplayedData/PerUserCorpus";
        //private string m_inputUri = "hog://Twitter/Corpus";
        private string m_outputUri = "hog://Output/Data";

        public SdcGrouper()
        {
            m_pigPen = SharedPigPen.GetPigPenForAssembly(this);
        }

        public void Start()
        {
            m_pigPen.Sooey<InputData>(m_inputUri);
            m_pigPen.Sooey<OutputData>(m_outputUri);
        }

        public void Stop()
        {
            if (null != m_inputPig)
            {
                m_inputPig.Close();
            }
        }

        public void HandlePig<TValue, TKey>(Pig<TValue, TKey> pig)
        {
            EventLog.Log("Got pig:"+pig);
            pig.Disconnected += (ignored1, ignored2) => EventLog.Log(pig + "disconnected");
            if (pig.PigConfig.Uri == m_inputUri)
            {
                m_inputPig = (Pig<InputData, string>)(object)pig;
                m_inputPig.SubscriptionEndLoad += (ignored1, ignored2) => m_reLayoutQueuer.Loaded();
                m_inputPig.AddAction = a =>
                                           {
                                               m_reLayoutQueuer.Add(a);
                                           };
                m_inputPig.ModifyAction = (m, token) => m_reLayoutQueuer.Modify(m);
                m_inputPig.DeleteAction = d => m_reLayoutQueuer.Delete(d);
                m_inputPig.Subscribe("top(20) and 1=1");//HACK
            }
            else if (pig.PigConfig.Uri == m_outputUri)
            {
                m_ouptutPig = (Pig<OutputData, string>)(object)pig;
                m_ouptutPig.SubscriptionEndLoad+=(ignored1,ignored2)=>m_reLayoutQueuer.AddOuputHog(m_ouptutPig);
                m_ouptutPig.Subscribe("1=1");
            }
        }


        public void NoSchema(IPigConfig pc)
        {
            if (pc.Uri == m_outputUri)
            {
                m_pigPen.RegisterSchema<OutputData, string>(pc, "Key");
            }
        }

        public void PigException(PigPen pigPen, PigExceptionEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
