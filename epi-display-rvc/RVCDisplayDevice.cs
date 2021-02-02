using System;
using System.Linq;
using System.Text;
// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronConnected;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace RVCDisplay
{
	/// <summary>
	/// Plugin device template for third party devices that use IBasicCommunication
	/// </summary>
    [Description("RoomView Connected Display Essential Device")]
    public class RVCDisplayDevice : TwoWayDisplayBase, IBasicVolumeWithFeedback, ICommunicationMonitor, IBridgeAdvanced
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        private RVCDisplayConfig _config;

        private RoomViewConnectedDisplay _display;

        /// <summary>
        /// CCDDisplay Plugin device constructor for ISerialComport transport
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        /// <param name="display">Loaded and initialized instance of CCD Display driver instance</param>
        public RVCDisplayDevice(string key, string name, RVCDisplayConfig config, RoomViewConnectedDisplay display)
            : base(key, name)
        {
            Debug.Console(0, this, "Constructing new {0} instance", name);

            _config = config;
            _display = display;

            StatusFeedback = new IntFeedback(() => (int)CommunicationMonitor.Status);
            Feedbacks.Add(StatusFeedback);

            VolumeLevelFeedback = new IntFeedback(() => { return (int)_display.VolumeFeedback.UShortValue; });
            MuteFeedback = new BoolFeedback(() => _display.MuteOnFeedback.BoolValue);
            Feedbacks.Add(VolumeLevelFeedback);
            Feedbacks.Add(MuteFeedback);

            CommunicationMonitor = new CrestronGenericBaseCommunicationMonitor(this, _display, 12000, 30000);

            for (uint i = 1; i <= _display.SourceSelectFeedbackSigs.Count; i++)
            {
                string sourceName = "input" + i.ToString();
                /// CompactFramework fix for inline Actions and using iterator variables
                uint sourceIndex = i;
                RoutingInputPort inputPort = new RoutingInputPort(sourceName, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, new Action(() => _display.SourceSelectSigs[sourceIndex].BoolValue = true), this)
                {
                    FeedbackMatchObject = sourceIndex
                };
                _display.SourceSelectSigs[sourceIndex].UserObject = inputPort;
                InputPorts.Add(inputPort);
            }

            CrestronConsole.AddNewConsoleCommand((s) => 
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Driver Information:");
                    sb.AppendFormat("\tDevice ID:        {0}\r\n", _display.DeviceIdStringFeedback.StringValue);
                    sb.AppendFormat("\tFirmware:         {0}\r\n", _display.FirmwareVersionFeedback.StringValue);
                    sb.AppendFormat("\tName:             {0}\r\n", _display.ProjectorNameFeedback.StringValue);
                    sb.AppendFormat("\tDescription:      {0}\r\n", _display.Description);
                    sb.AppendFormat("\tStatus:           {0}\r\n", _display.StatusMessageFeedback.StringValue);
                    sb.AppendFormat("\tLamp:             {0}\r\n", _display.LampHoursFeedback.UShortValue);
                    sb.AppendFormat("\tLamp (text):      {0}\r\n", _display.LampHoursTextFeedback.StringValue);

                    CrestronConsole.ConsoleCommandResponse("{0}", sb.ToString());
                },
                Key + "INFO", "Print Driver Info", ConsoleAccessLevelEnum.AccessOperator);

            CrestronConsole.AddNewConsoleCommand((s) =>
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("State:");
                    sb.AppendFormat("\tName:             {0}\r\n", _display.Name);
                    sb.AppendFormat("\tID:               {0}\r\n", _display.ID);
                    sb.AppendFormat("\tOnline:           {0}\r\n", _display.IsOnline?"Online":"Offline");
                    sb.AppendFormat("\tPower:            {0}\r\n", _display.PowerOnFeedback.BoolValue?"ON":"OFF");
                    sb.AppendFormat("\tCooling:          {0}\r\n", _display.CoolingDownFeedback.BoolValue ? "ON" : "OFF");
                    sb.AppendFormat("\tWarming:          {0}\r\n", _display.WarmingUpFeedback.BoolValue ? "ON" : "OFF");
                    sb.AppendFormat("\tMute:             {0}\r\n", _display.MuteOnFeedback.BoolValue ? "ON" : "OFF");
                    sb.AppendFormat("\tVolume:           {0}\r\n", _display.VolumeFeedback.UShortValue);
                    sb.AppendFormat("\tLamp:             {0}\r\n", _display.LampHoursFeedback.UShortValue);

                    CrestronConsole.ConsoleCommandResponse("{0}", sb.ToString());
                },
                Key + "STATE", "Print display state", ConsoleAccessLevelEnum.AccessOperator);

            CrestronConsole.AddNewConsoleCommand((s) =>
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("Current Input:  {0}\r\n", _display.CurrentSourceFeedback.StringValue);
                    sb.AppendFormat("Inputs:\r\n");
                    for (uint i = 1; i <= _display.SourceSelectFeedbackSigs.Count; i++)
                    {
                        string sourceName = _display.SourceNameTextFeedbackSigs[i].StringValue;
                        if (String.IsNullOrEmpty(sourceName) || String.IsNullOrEmpty(sourceName.Trim()))
                            break;
                        sb.AppendFormat("\t{0}: {1}\r\n", sourceName, _display.SourceSelectFeedbackSigs[i].BoolValue ? "ON" : "");
                    }

                    CrestronConsole.ConsoleCommandResponse(sb.ToString());
                },
                Key + "INPUTS", "Display Driver Inputs", ConsoleAccessLevelEnum.AccessOperator);

            CrestronConsole.AddNewConsoleCommand((s) =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Input Ports:\r\n");
                foreach (var inputPort in InputPorts)
                {
                    //uint sourceSelectIndex = _display.SourceSelectSigs.FirstOrDefault<BoolInputSig>(sig => sig.UserObject == inputPort).Number;
                    uint sourceSelectIndex = 0;
                    for (uint i = 1; i <= _display.SourceSelectSigs.Count; i++)
                    {
                        if (_display.SourceSelectSigs[i].UserObject == inputPort)
                        {
                            sourceSelectIndex = i;
                            break;
                        }
                    }
                    sb.AppendFormat("\t{0}: {1}\r\n", inputPort.Key, _display.SourceNameTextFeedbackSigs[sourceSelectIndex].StringValue);
                }

                CrestronConsole.ConsoleCommandResponse(sb.ToString());
            },
                Key + "ROUTINGPORTS", "Display Driver Routing Ports", ConsoleAccessLevelEnum.AccessOperator);
        }

        private void displayEvent(GenericBase device, BaseEventArgs args)
        {
            switch (args.EventId)
            {
                case RoomViewConnectedDisplay.OnLineFeedbackEventId:
                    // TODO: figure out what to do with OnLine event in BaseEvent
                    // Read current state of the projector power and if differ from room state update room state accordingly without setters actions.
                    foreach (var feeedback in Feedbacks)
                    {
                        //feeedback.FireUpdate();
                    }
                    break;

                case RoomViewConnectedDisplay.PowerOffFeedbackEventId:
                case RoomViewConnectedDisplay.PowerOnFeedbackEventId:
                    PowerIsOnFeedback.FireUpdate();
                    break;

                case RoomViewConnectedDisplay.CoolingDownFeedbackEventId:
                    IsCoolingDownFeedback.FireUpdate();
                    break;

                case RoomViewConnectedDisplay.WarmingUpFeedbackEventId:
                    IsWarmingUpFeedback.FireUpdate();
                    break;

                case RoomViewConnectedDisplay.MuteOnFeedbackEventId:
                    MuteFeedback.FireUpdate();
                    break;

                case RoomViewConnectedDisplay.VolumeFeedbackEventId:
                    VolumeLevelFeedback.FireUpdate();
                    break;

                case RoomViewConnectedDisplay.SourceSelectFeedbackEventId:
                    uint sourceSelectIndex = (uint)args.Index;
                    if (_display.SourceSelectFeedbackSigs[sourceSelectIndex].BoolValue == true)
                    {
                        RoutingInputPort newInputPort = (RoutingInputPort)_display.SourceSelectFeedbackSigs[sourceSelectIndex].UserObject;
                        CurrentInputFeedback.FireUpdate();
                        OnSwitchChange(new RoutingNumericEventArgs(null, newInputPort, eRoutingSignalType.AudioVideo));
                    }
                    break;

                case RoomViewConnectedDisplay.SourceNameTextFeedbackEventId:
                    break;

                case RoomViewConnectedDisplay.LampHoursFeedbackEventId:
                case RoomViewConnectedDisplay.LampHoursTextFeedbackEventId:
                    break;
            }
        }

        /// <summary>
        /// Registers the Crestron device, connects up to the base events, starts communication monitor
        /// </summary>
        public override bool CustomActivate()
        {
            Debug.Console(0, this, "Activating");
            if (!base.CustomActivate())
                return false;

            var response = _display.RegisterWithLogging(Key);
			if (response != eDeviceRegistrationUnRegistrationResponse.Success)
			{
			    Debug.Console(0, this, "ERROR: Cannot register Crestron device: {0}", response);
                return false;
            }

            // TODO: implement IsRegestered feedback
            //IsRegistered.FireUpdate();

            foreach (var f in Feedbacks)
            {
                f.FireUpdate();
            }

            _display.BaseEvent += displayEvent;
            _display.OnlineStatusChange += onlineStatusChange;
            CommunicationMonitor.Start();

            return true;
        }

        /// <summary>
        /// This disconnects events and unregisters the base hardware device.
        /// </summary>
        public override bool Deactivate()
        {
            if (!base.Deactivate())
                return false;

            CommunicationMonitor.Stop();

            CommunicationMonitor.Stop();
            _display.OnlineStatusChange -= onlineStatusChange;
            _display.BaseEvent -= displayEvent;

            var success = _display.UnRegister() == eDeviceRegistrationUnRegistrationResponse.Success;

            // TODO: implement IsRegistered feedback
            //IsRegistered.FireUpdate();

            return success;
        }

        void onlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            Debug.Console(2, this, "OnlineStatusChange Event.  Online = {0}", args.DeviceOnLine);
            foreach (var feedback in Feedbacks)
            {
                if (feedback != null)
                    feedback.FireUpdate();
            }
        }

        private uint getCurrentInputIndex()
        {
            uint sourceSelectIndex = 0;
            for (uint i = 1; i <= _display.SourceSelectSigs.Count; i++)
            {
                if (_display.SourceSelectSigs[i].BoolValue == true)
                {
                    sourceSelectIndex = i;
                    break;
                }
            }
            return sourceSelectIndex;
        }

        #region TwoWayDisplayBase abstract class overrides

        protected override Func<bool> PowerIsOnFeedbackFunc { get { return () => _display.PowerOnFeedback.BoolValue; } }

        protected override Func<string> CurrentInputFeedbackFunc { get { return () => "input"+getCurrentInputIndex().ToString(); } }

        // TwoWayDisplayBase: IHasPowerControlWithFeedback: IHasPowerControl.PowerOff
        public override void PowerOff() { _display.PowerOff(); }

        // TwoWayDisplayBase: IHasPowerControlWithFeedback: IHasPowerControl.PowerOn
        public override void PowerOn() { _display.PowerOn(); }

        // TwoWayDisplayBase: IHasPowerControlWithFeedback: IHasPowerControl.PowerToggle
        public override void PowerToggle()
        {
            if(_display.PowerOnFeedback.BoolValue)
                _display.PowerOff();
            else
                _display.PowerOn();
        }

        #endregion

        #region DisplayBase abstract class overrides

        protected override Func<bool> IsCoolingDownFeedbackFunc { get { return () => _display.CoolingDownFeedback.BoolValue; } }

        protected override Func<bool> IsWarmingUpFeedbackFunc { get { return () => _display.WarmingUpFeedback.BoolValue; } }

        public override void ExecuteSwitch(object selector)
        {
            var handler = selector as Action;
            if (handler != null)
                handler();
        }

        #endregion

        #region IBasicVolumeWithFeedback Members

        /// <summary>
        /// Provides feedback of current mute state
        /// </summary>
        public BoolFeedback MuteFeedback { get; private set; }

        /// <summary>
        /// Unmutes the display
        /// </summary>
        public void MuteOff()
        {
            _display.MuteOff();
        }

        /// <summary>
        /// Mutes the display
        /// </summary>
        public void MuteOn()
        {
            _display.MuteOn();
        }

        /// <summary>
        /// Provides feedback of current volume level
        /// </summary>
        public IntFeedback VolumeLevelFeedback { get; private set; }

        /// <summary>
        /// Set current volume level
        /// </summary>
        public void SetVolume(ushort level)
        {
            _display.Volume.UShortValue = level;
        }

        #endregion

        #region IBasicVolumeControls Members

        public void MuteToggle()
        {
            _display.MuteToggle();
        }

        public void VolumeDown(bool pressRelease)
        {
            // TODO: Volume Down Press and Hold
            if (pressRelease)
            {
                _display.VolumeDown();
            }
        }

        public void VolumeUp(bool pressRelease)
        {
            // TODO: Volume Up Press and Hold
            if (pressRelease)
            {
                _display.VolumeUp();
            }
        }

        #endregion

        #region ICommunicationMonitor Members

        public StatusMonitorBase CommunicationMonitor { get; private set;}

        #endregion

        #region IBridgeAdvanced Members

        /// <summary>
        /// Reports socket status feedback through the bridge
        /// </summary>
        public IntFeedback StatusFeedback { get; private set; }

        /// <summary>
        /// Links the plugin device to the EISC bridge
        /// </summary>
        /// <param name="trilist"></param>
        /// <param name="joinStart"></param>
        /// <param name="joinMapKey"></param>
        /// <param name="bridge"></param>
        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new RVCDisplayBridgeJoinMap(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            // TODO: figure out how best way to handle base and override class maps and ranges
            LinkDisplayToApi(this, trilist, joinStart, joinMapKey, null);

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

            // links to bridge

            /// eJoinCapabilities.ToFromSIMPL - FromSIMPL action
            //trilist.SetBoolSigAction(joinMap.Connect.JoinNumber, sig => Connect = sig);
            /// eJoinCapabilities.ToFromSIMPL - ToSIMPL subscription
            //ConnectFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Connect.JoinNumber]);

            /// eJoinCapabilities.ToFromSIMPL - ToSIMPL subscription
            StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);

            /// eJoinCapabilities.ToSIMPL - set string once as this is not changeble info
            trilist.SetString(joinMap.Driver.JoinNumber, _display.GetType().AssemblyQualifiedName);

            UpdateFeedbacks();

            /// Propagate String/Serial values through eisc when it becomes online 
            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;

                trilist.SetString(joinMap.Driver.JoinNumber, _display.GetType().AssemblyQualifiedName);
                UpdateFeedbacks();
            };
        }

        private void UpdateFeedbacks()
        {
            StatusFeedback.FireUpdate();
        }

        #endregion

    }
}

