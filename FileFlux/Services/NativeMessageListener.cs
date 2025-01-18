using System;
using System.IO;

namespace FileFlux.Services
{
    public sealed class NativeMessageListener
    {
        private static readonly Lazy<NativeMessageListener> _instance =
            new Lazy<NativeMessageListener>(() => new NativeMessageListener());

        private bool _isListening;

        public static NativeMessageListener Instance => _instance.Value;

        private NativeMessageListener() { }

        public event EventHandler<string> MessageReceived;

        public void StartListening()
        {
            if (_isListening) return;

            _isListening = true;
            ListenForMessages();
        }

        public void StopListening()
        {
            _isListening = false;
        }

        private void ListenForMessages()
        {
            while (_isListening)
            {
                using (var stdin = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
                using (var stdout = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding))
                {
                    var input = stdin.ReadLine();
                    if (input == null)
                    {
                        continue;
                    }

                    OnMessageReceived(input);

                    var response = ProcessMessage(input);

                    stdout.WriteLine(response);
                    stdout.Flush();
                }
            }
        }

        private void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }

        private string ProcessMessage(string message)
        {
            // Your logic to process the incoming message
            return $"Processed: {message}";
        }
    }
}
