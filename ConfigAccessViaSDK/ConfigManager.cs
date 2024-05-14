using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using VideoOS.Platform;
using VideoOS.Platform.Messaging;

namespace ConfigAccessViaSDK
{
    public class ConfigManager
    {
        private MessageCommunication _messageCommunication;
        private object _systemConfigurationChangedIndicationReference;
        private Timer _catchUpTimer;

        public void Init()
        {
            _catchUpTimer = new Timer(CatchUpTimerHandler);

            try
            {
                MessageCommunicationManager.Start(EnvironmentManager.Instance.MasterSite.ServerId);
                _messageCommunication = MessageCommunicationManager.Get(EnvironmentManager.Instance.MasterSite.ServerId);

                _systemConfigurationChangedIndicationReference = _messageCommunication.RegisterCommunicationFilter(SystemConfigChangedHandler,
                    new CommunicationIdFilter(MessageId.System.SystemConfigurationChangedIndication));

            }
            catch (MIPException ex)
            {
                Trace.WriteLine("Message Communcation not supported:" + ex.Message);
            }
        }

        public void Close()
        {
            _messageCommunication.UnRegisterCommunicationFilter(_systemConfigurationChangedIndicationReference);
            MessageCommunicationManager.Stop(EnvironmentManager.Instance.MasterSite.ServerId);
        }
        private void CatchUpTimerHandler(object o)
        {
            _catchUpTimer.Change(Timeout.Infinite, Timeout.Infinite);

            VideoOS.Platform.SDK.Environment.ReloadConfiguration(Configuration.Instance.ServerFQID);
        }

        private object SystemConfigChangedHandler(Message message, FQID dest, FQID source)
        {
            List<FQID> fqids = message.Data as List<FQID>;
            if (fqids == null)
            {  
                _catchUpTimer.Change(TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(90));
                return null;
            }

            _catchUpTimer.Change(Timeout.Infinite, Timeout.Infinite);

            HashSet<FQID> serverFQIDList = new HashSet<FQID>();
            foreach (FQID fqid in fqids)
            {
                Item item = Configuration.Instance.GetItem(fqid);
                if (item != null)
                {
                    Trace.WriteLine("SystemConfigurationChangedIndication - received -- for: " + item.Name);
                    FQID recorderFQID;
                    if (fqid.Kind == Kind.Server)
                        recorderFQID = fqid;
                    else
                        recorderFQID = fqid.GetParent();
                    if (recorderFQID != null)
                        serverFQIDList.Add(recorderFQID);
                }
                else
                {
                    Trace.WriteLine("SystemConfigurationChangedIndication - received -- for: Unknown Item");
                    serverFQIDList.Clear();
                    serverFQIDList.Add(Configuration.Instance.ServerFQID);
                    break;
                }
            }

            Thread reloadThread = new Thread(new ParameterizedThreadStart(ReloadConfigurationThread));
            reloadThread.Start(serverFQIDList);

            return null;
        }

        private void ReloadConfigurationThread(object obj)
        {
            HashSet<FQID> serverFQIDList = obj as HashSet<FQID>;
            if (serverFQIDList != null)
            {
                foreach (FQID serverFQID in serverFQIDList)
                    VideoOS.Platform.SDK.Environment.ReloadConfiguration(serverFQID);
            }

        }
    }
}