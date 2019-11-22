using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gmail
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            /* BASE ON https://developers.google.com/gmail/api/quickstart/dotnet */

            Console.WriteLine("Gmail API");
            Console.WriteLine("================================");
            try
            {
                new Program().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private async Task Run()
        {
            UserCredential credential;
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                // NOTE: This app needs google verification
                // https://support.google.com/cloud/answer/7454865?hl=en&ref_topic=3473162
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { GmailService.Scope.GmailReadonly },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("token.json", true));
            }

            // Create Gmail API service.
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Gmail API .NET Quickstart",
            });

            // Batch the list all messages.
            var messages = await BatchMessagesAsync(service, "me");

            // List all messages.
            //var messages = await ListMessagesAsync(service, "me");
            //Console.WriteLine("Messages:");
            //if (messages != null && messages.Count > 0)
            //{
            //    foreach (var messageItem in messages)
            //    {
            //        var message = await GetMessagesAsync(service, messageItem.Id);

            //        Console.WriteLine("{0} {1}", message.Id, message.Snippet);
            //    }
            //}
            //else
            //{
            //    Console.WriteLine("No messages found.");
            //}
            Console.Read();
        }

        /// <summary>
        /// List all Messages of the user's mailbox matching the query.
        /// </summary>
        /// <param name="service">Gmail API service instance.</param>
        /// <param name="userId">User's email address. The special value "me"
        /// can be used to indicate the authenticated user.</param>
        /// <param name="filter">string used to filter Messages returned.</param>
        public async Task<List<Message>> ListMessagesAsync(GmailService service, string userId, string filter = "")
        {
            var result = new List<Message>();

            // Define parameters of request.
            var request = service.Users.Messages.List(userId);
            request.Q = filter;

            do
            {
                try
                {
                    var response = await request.ExecuteAsync();
                    result.AddRange(response.Messages);
                    request.PageToken = response.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            } while (!string.IsNullOrEmpty(request.PageToken));

            return result;
        }

        /// <summary>
        /// Get a Message of the user's mailbox matching the id.
        /// </summary>
        /// <param name="service">Gmail API service instance.</param>
        /// <param name="messageId">The message id must be returned.</param>
        /// <returns>The Message full.</returns>
        public async Task<Message> GetMessagesAsync(GmailService service, string messageId)
        {
            var request = service.Users.Messages.Get("me", messageId);
            request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;

            var message = await request.ExecuteAsync();

            return message;
        }

        /// <summary>
        /// Barch the list all Messages of the user's mailbox matching the query.
        /// </summary>
        /// <param name="service">Gmail API service instance.</param>
        /// <param name="userId">User's email address. The special value "me"
        /// can be used to indicate the authenticated user.</param>
        /// <param name="filter">string used to filter Messages returned.</param>
        public async Task<Message[]> BatchMessagesAsync(GmailService service, string userId, string filter = "")
        {
            var messageList = new Dictionary<string, Message>();

            // Define parameters of request.
            var listRequest = service.Users.Messages.List(userId);
            listRequest.Q = filter;
            listRequest.MaxResults = 500; // Limit of messages to return (500 maximum)

            Console.WriteLine("Messages:");

            do
            {
                // Execute the request for return all messages 
                try
                {
                    // Batch for each page returned by list (1000 maximum)
                    var batchRequest = new BatchRequest(service);

                    var response = await listRequest.ExecuteAsync();

                    foreach (var messageItem in response.Messages)
                    {
                        messageList.Add(messageItem.Id, messageItem);

                        // Create a get request for return the full message
                        var messageRequest = service.Users.Messages.Get("me", messageItem.Id);
                        messageRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;

                        // Set the request and callback
                        batchRequest.Queue<Message>(
                            messageRequest,
                            (message, error, i, msg) =>
                            {
                                if (error == null)
                                {
                                    messageList[message.Id] = message;

                                    Console.WriteLine("{0} {1}", message.Id, message.Snippet);
                                }
                                else
                                {
                                    RequestError requestError = error;

                                    Console.WriteLine("ERROR: {0}", requestError.Message);
                                }
                                Console.WriteLine();
                            }
                        );
                    }

                    // Set the next page
                    listRequest.PageToken = response.NextPageToken;


                    await batchRequest.ExecuteAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            } while (!string.IsNullOrEmpty(listRequest.PageToken));

            if (messageList.Count() == 0)
            {
                Console.WriteLine("No messages found.");
            }


            return messageList.Values.ToArray();
        }
    }
}
