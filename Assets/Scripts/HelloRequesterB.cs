using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading;

/// <summary>
///     Example of requester who only sends Hello. Very nice guy.
///     You can copy this class and modify Run() to suits your needs.
///     To use this class, you just instantiate, call Start() when you want to start and Stop() when you want to stop.
/// </summary>
public class HelloRequesterB : RunAbleThread
{
    /// <summary>
    ///     Request Hello message to server and receive message back. Do it 10 times.
    ///     Stop requesting when Running=false.
    /// </summary>
    public readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
    public bool bool_SendComplete;
    public bool bool_RecvComplete;
    public string str_message_send; // send 
    public string str_message_recv; 
    protected override void Run()
    {
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
        using (RequestSocket client = new RequestSocket())
        {
            client.Connect("tcp://localhost:1239");
            
            while (Running)
            {
              
                if (bool_SendComplete == true && str_message_send != null)
                {
                    //Debug.Log("Sending control input");
                    client.SendFrame(str_message_send);
                    // ReceiveFrameString() blocks the thread until you receive the string, but TryReceiveFrameString()
                    // do not block the thread, you can try commenting one and see what the other does, try to reason why
                    // unity freezes when you use ReceiveFrameString() and play and stop the scene without running the server
                    //                string message = client.ReceiveFrameString();
                    //                Debug.Log("Received: " + message);
                    string message = null;
                    bool gotMessage = false;
                    
                    //receving procedure
                    bool_RecvComplete = false;
                    while (Running)
                    {
                        gotMessage = client.TryReceiveFrameString(out message); // this returns true if it's successful
                        //message = client.ReceiveFrameString(); // this returns true if it's successful
                        //gotMessage = true;
                        if (gotMessage)
                        {
                            var splittedStrings = message.Split(' ');
                            if (splittedStrings[0] == "Hello")
                            {
                                str_message_recv = message;
                                _messageQueue.Enqueue(message);
                                bool_RecvComplete = true; // send true if receiving is finished.
                                bool_SendComplete = false; // send only 1 time.
                                //Thread.Sleep(3);
                                break;
                            }
                            
                        }
                        else
                        {
                            bool_RecvComplete = false;
                        }
                      
                    }
                    //if (gotMessage) Debug.Log("Received " + message);
                }
                
            }
            client.Close();
        }

        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }

    public string DataPostProcessing()
    {
        //3. messageQue 에 값을 넣는다.
        if (!_messageQueue.IsEmpty)
        {
            string message;
            bool test = _messageQueue.TryDequeue(out message);
            //message = str_message_recv;
            // message 첫 시작이 포즈 시작 트리거일 때
            if (message != null)
            {
                //_messageDelegate(message);
                //Debug.Log("Receving " + _messageQueue.Count);
                return message;
            }
            else
            {
                return "F";
            }
        }
        else
        {
            return "F";
        }
    }
}