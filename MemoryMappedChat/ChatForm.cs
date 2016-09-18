using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace MemoryMappedChat
{
    public partial class ChatForm : Form
    {
        const int kMessageByteSize = 1024;
        const int kMessagesLimit = 30;
        const int kFileSize = 102400;
        const float kQueringPeriod = 0.3f;

        BackgroundWorker gettingWorker = new BackgroundWorker();
        MemoryMappedFile mmf;
        string currentUserName;
        int messageCounter = 0;

        public ChatForm()
        {
            InitializeComponent();
            {
            }
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {
            messageCounter = 0;
            try
            {
                mmf = MemoryMappedFile.OpenExisting("chatFile");
                UpdateHistory();
            }
            catch (FileNotFoundException)
            {
                mmf = MemoryMappedFile.CreateFromFile(@"d:\chatFile.txt", FileMode.OpenOrCreate, "chatFile", kFileSize);
                UpdateHistory();
            }

            gettingWorker.DoWork += gettingWorker_DoWork;
            gettingWorker.RunWorkerAsync();

            string[] names = {"John", "Jack", "Steven", "Martha", "Maria", "Joshua", "Phillip"};

            Random rnd = new Random();

            currentUserName = names[rnd.Next(0, 6)];

            MessageBox.Show("Your random username is: " + currentUserName);
        }

        void gettingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep((int)(kQueringPeriod * 1000));
                UpdateHistory();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (messageCounter < kMessagesLimit)
            {
                string currentText = textBoxMessage.Text;

                if (currentText.Count() > 0)
                {
                    ChatMessage message = new ChatMessage();
                    message.Text = currentText;
                    message.Time = DateTime.Now;
                    message.Username = currentUserName;
                    BinaryFormatter bf = new BinaryFormatter();
                    byte[] buf = new byte[1024];

                    using (var ms = new MemoryStream())
                    {
                        bf.Serialize(ms, message);
                        buf = ms.ToArray();
                    }

                    using (var accessor = mmf.CreateViewAccessor())
                    {
                        accessor.WriteArray(messageCounter * kMessageByteSize, buf, 0, buf.Count());
                    }

                    textBoxMessage.Clear();
                    textBoxMessage.Focus();
                }
            }
            else
            {
                MessageBox.Show(@"Limit of messages exceeded!!");
            }
        }

        private void UpdateHistory()
        {
            byte[] buf = new byte[1024];
            ChatMessage message = new ChatMessage();
            BinaryFormatter bf = new BinaryFormatter();

            using (var accessor = mmf.CreateViewAccessor())
            {
                int count = messageCounter;
                do
                {
                    accessor.ReadArray(count*kMessageByteSize, buf, 0, 1024);
                    if (buf.All(singleByte => singleByte == 0))
                    {
                        break;
                    }
                    count++;

                    using (var ms = new MemoryStream(buf))
                    {
                        message = (ChatMessage)bf.Deserialize(ms);
                    }

                    string time = message.Time.ToShortTimeString();

                    textBoxChat.Invoke((Action)(() =>
                        {
                            if (count == 1)
                            {
                                textBoxChat.Text += String.Format("{0} at {1}: {2}", message.Username, time, message.Text);
                            }
                            else
                            {
                                textBoxChat.Text += String.Format("\r\n{0} at {1}: {2}", message.Username, time, message.Text);
                            }
                            textBoxChat.SelectionStart = textBoxChat.Text.Length;
                            textBoxChat.ScrollToCaret();
                        }
                    ));
                }
                while (!(buf.All(singleByte => singleByte == 0)) && count<kMessagesLimit);

                messageCounter = count;
            }
        }
    }
}
