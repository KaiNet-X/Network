using System;

namespace Net.Messages
{
    public class ChannelManagementMessage : MessageBase
    {
        public override string MessageType => "Channel";
        public Guid Id { get; set; }
        public int Port { get; set; }
        public Mode ManageMode { get; set; }

        public ChannelManagementMessage(Guid guid, int port, Mode mode)
        {
            RegisterMessage();
            Id = guid;
            Port = port;
            ManageMode = mode;
        }

        public ChannelManagementMessage(Guid guid, Mode mode)
        {
            RegisterMessage();
            Id = guid;
            ManageMode = mode;
        }

        public ChannelManagementMessage()
        {

        }

        protected internal override object GetValue() => Id;

        public enum Mode
        {
            Create,
            Confirm
        }
    }
}
