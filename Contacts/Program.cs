using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;

namespace Contacts
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Contact API");
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
            using (var stream = new FileStream("json file", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { "profile", "https://www.googleapis.com/auth/contacts.readonly" },
                    "user", CancellationToken.None);
            }

            var service = new PeopleServiceService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Google App Name",
            });

            ContactGroupsResource groupsResource = new ContactGroupsResource(service);
            ContactGroupsResource.ListRequest listRequest = groupsResource.List();
            ListContactGroupsResponse response = listRequest.Execute();


            List<string> groupNames = new List<string>();
            foreach (ContactGroup group in response.ContactGroups)
            {
                groupNames.Add(group.FormattedName);
            }

            PeopleResource.ConnectionsResource.ListRequest peopleRequest =
                service.People.Connections.List("people/me");
            peopleRequest.PersonFields = "names,emailAddresses";
            peopleRequest.SortOrder = (PeopleResource.ConnectionsResource.ListRequest.SortOrderEnum)1;
            ListConnectionsResponse people = peopleRequest.Execute();

            List<string> contacts = new List<string>();
            foreach (var person in people.Connections)
            {
                Console.WriteLine(person.Names?[0]?.DisplayName);

            }


        }
    }
}
