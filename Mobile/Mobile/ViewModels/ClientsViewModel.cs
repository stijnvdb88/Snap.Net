//using System;
//using System.Collections.ObjectModel;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Xamarin.Forms;
//using Mobile.Models;
//using Mobile.Views;
//using SnapDotNet.ControlClient;
//using SnapDotNet.ControlClient.JsonRpcData;

//namespace Mobile.ViewModels
//{
//    public class ClientsViewModel : BaseViewModel
//    {
//        public ObservableCollection<Group> Groups { get; set; }
//        public Command LoadItemsCommand { get; set; }

//        public ClientsViewModel()
//        {
//            Title = "Clients";
//            Groups = new ObservableCollection<Group>();
//            LoadItemsCommand = new Command(async () => await ExecuteLoadClientsCommand());

//            //MessagingCenter.Subscribe<NewItemPage, Item>(this, "AddItem", async (obj, item) =>
//            //{
//            //    var newItem = item as Item;
//            //    Clients.Add(newItem);
//            //    await DataStore.AddItemAsync(newItem);
//            //});
//        }

//        async Task ExecuteLoadClientsCommand()
//        {
//            IsBusy = true;

//            try
//            {
//                Groups.Clear();
//                var items = await DataStore.GetItemsAsync(true);
//                ////SnapcastClient client = App.Instance.SnapcastClient;
//                ////var data = await client.GetServerStatusAsync();
//                //foreach (var item in client.ServerData.groups)
//                //{
//                //    Groups.Add(item);
//                //    Debug.WriteLine(item.name);
//                //}
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine(ex);
//            }
//            finally
//            {
//                IsBusy = false;
//            }
//        }
//    }
//}