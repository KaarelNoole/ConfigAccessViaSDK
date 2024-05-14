using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VideoOS.Platform;
using VideoOS.Platform.Login;
using VideoOS.Platform.Messaging;
using VideoOS.Platform.UI;
using VideoOS.Platform.UI.Controls;

namespace ConfigAccessViaSDK
{

    public partial class ConfigAccess : VideoOSWindow
    {
        private ConfigManager _configManager = new ConfigManager();
        private object _localConfigurationChangedIndicationReference;

        public ConfigAccess()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            _dumpConfigurationUC.FillContent();
            _dumpConfigurationUC.FillDisabledItems();

            _configManager = new ConfigManager();
            _configManager.Init();

            EnvironmentManager.Instance.EnvironmentOptions[EnvironmentOptions.ConfigurationChangeCheckInterval] = "10";

            _localConfigurationChangedIndicationReference = EnvironmentManager.Instance.RegisterReceiver(LocalConfigUpdatedHandler,
                new MessageIdFilter(MessageId.System.LocalConfigurationChangedIndication));
            _localConfigurationChangedIndicationReference = EnvironmentManager.Instance.RegisterReceiver(ConfigUpdatedHandler,
                new MessageIdFilter(MessageId.Server.ConfigurationChangedIndication));
        }

        private object LocalConfigUpdatedHandler(Message message, FQID dest, FQID source)
        {
            _dumpConfigurationUC.FillContentSpecific((bool)_dumpConfigurationUC._physical_CheckBox.IsChecked ? ItemHierarchy.SystemDefined : ItemHierarchy.UserDefined);
            return null;
        }

        private object ConfigUpdatedHandler(Message message, FQID dest, FQID source)
        {
            _dumpConfigurationUC.ShowInfo("MIP config changed: " + message.RelatedFQID.Kind);
            return null;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            VideoOS.Platform.SDK.Environment.Logout();
            _dumpConfigurationUC.Clear();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            LoginSettings ls = LoginSettingsCache.GetLoginSettings(EnvironmentManager.Instance.MasterSite);
            VideoOS.Platform.SDK.Environment.Login(
                ls.Uri, App.integrationId,
                App.integrationName,
                App.manufacturerName,
                App.version,
                true);
            _dumpConfigurationUC.FillContent();
        }

        private void ShowLicense(object sender, EventArgs e)
        {
            string lic = "SLC: " + EnvironmentManager.Instance.SystemLicense.SLC + Environment.NewLine +
             "Expire: " + EnvironmentManager.Instance.SystemLicense.Expire.ToLongDateString() + Environment.NewLine;
            foreach (String feature in EnvironmentManager.Instance.SystemLicense.FeatureFlags)
            {
                lic += "Feature: " + feature + Environment.NewLine;
            }
            lic += "ProductCode: " + EnvironmentManager.Instance.SystemLicense.ProductCode + Environment.NewLine;
            foreach (String key in EnvironmentManager.Instance.SystemLicense.ExpirationDateTimes.Keys)
            {
                lic += "Expiration of:" + key + " is " + EnvironmentManager.Instance.SystemLicense.ExpirationDateTimes[key].ToLongDateString() + Environment.NewLine;
            }
            VideoOSMessageBox.Show(
                this,
                "License",
                "License",
                $"{lic}",
                VideoOSMessageBox.Buttons.OK);
        }

        private void OnClose(object sender, EventArgs e)
        {
            EnvironmentManager.Instance.UnRegisterReceiver(_localConfigurationChangedIndicationReference);
            _configManager.Close();
            Close();
        }

        private void triggerButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _dumpConfigurationUC.GetSelectedItem();
            if (item == null)
            {
                VideoOSMessageBox.Show(
                    this,
                    "Item missing",
                    "Item Missing",
                    "Select an item",
                    VideoOSMessageBox.Buttons.OK);
                return;
            }

            if (item.FQID.Kind == Kind.TriggerEvent && item.FQID.ServerId.ServerType == ServerId.CorporateManagementServerType)
            {
                ItemPickerWpfWindow itemPicker = new ItemPickerWpfWindow();
                itemPicker.KindsFilter = new List<Guid> { Kind.Camera };
                itemPicker.Items = Configuration.Instance.GetItems();

                if (itemPicker.ShowDialog().Value)
                {
                    EnvironmentManager.Instance.PostMessage(
                        new Message(
                            MessageId.Control.TriggerCommand,
                            itemPicker.SelectedItems.First().FQID
                        ),
                        item.FQID
                    );

                    return;
                }
                else
                {
                    EnvironmentManager.Instance.PostMessage(
                        new Message(
                            MessageId.Control.TriggerCommand
                        ),
                        item.FQID
                    );

                    return;
                }
            }

            if (item.FQID.Kind == Kind.Matrix)
            {
                ItemPickerWpfWindow itemPicker = new ItemPickerWpfWindow();
                itemPicker.KindsFilter = new List<Guid> { Kind.Matrix };
                itemPicker.Items = Configuration.Instance.GetItems();

                if (itemPicker.ShowDialog().Value)
                {
                    EnvironmentManager.Instance.PostMessage(
                        new Message(
                            MessageId.Control.TriggerCommand,
                            itemPicker.SelectedItems.First().FQID
                        ),
                        item.FQID
                    );
                    return;
                }
                else
                {
                    VideoOSMessageBox.Show(
                        this,
                        "Missing camera trigger",
                        "Missing camera trigger",
                        "To trigger matrix without a camera is not useful.",
                        VideoOSMessageBox.Buttons.OK);
                    return;
                }
            }

            EnvironmentManager.Instance.PostMessage(
                        new Message(
                            MessageId.Control.TriggerCommand), item.FQID);
        }
    }
}