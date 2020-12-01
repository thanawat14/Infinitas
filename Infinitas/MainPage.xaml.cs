using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Nethereum.Contracts;
using Nethereum.HdWallet;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Xamarin.Forms;

namespace Infinitas
{
    public partial class MainPage : ContentPage
    {

        // Smart Contract Transfering and Checking
        string seed;
        string endpoint;
        string erc20ContractAddress;
        string erc20ContractAbi;
        EtherWallet wallet;
        Account account;
        Web3 web3;
        Contract erc20Contract;

        // Create a new MQTT client.
        MqttFactory factory;
        IMqttClient mqttClient;

        // Create TCP based options using the builder.
        IMqttClientOptions options;

        public MainPage()
        {
            InitializeComponent();
            scanButton.Clicked += ScanButton_Clicked;
            resultEntry.TextChanged += ResultEntry_TextChanged;

            qrcodeImageView.BarcodeOptions = new ZXing.Common.EncodingOptions { Height = 200, Width = 200 };

            seed = "calm like predict rib country globe small nation festival divert liquid lonely";
            endpoint = "https://rinkeby.infura.io/v3/77dd6590a15e41c688381e7ab2c28719";
            erc20ContractAddress = "0x73447F2EA765B3e9e3994D2852dcC7E420940300";
            erc20ContractAbi = "[{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"uint256\",\"name\":\"ref\",\"type\":\"uint256\"},{\"indexed\":false,\"internalType\":\"uint256\",\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"account\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"totalSupply\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"recipient\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"recipient\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";
            wallet = new EtherWallet(seed, null);
            account = wallet.GetAccount(0);
            web3 = new Web3(account, endpoint);
            erc20Contract = web3.Eth.GetContract(erc20ContractAbi, erc20ContractAddress);

            // Create a new MQTT client.
            factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            // Create TCP based options using the builder.
            options = new MqttClientOptionsBuilder()
                    .WithClientId("Macbook")
                    .WithTcpServer("34.87.129.55", 1883)
                    .WithCredentials("device", "123456")
                    .WithCleanSession()
                    .Build();

        }

        protected override async void OnAppearing()
        {
            await mqttClient.ConnectAsync(options, CancellationToken.None);

        }

        private void ResultEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            qrcodeImageView.BarcodeValue = string.IsNullOrEmpty(resultEntry.Text) ? "hello" : resultEntry.Text;
        }

        private async void ScanButton_Clicked(object sender, EventArgs e)
        {
            // QRcode scanning
            var scanner = new ZXing.Mobile.MobileBarcodeScanner();
            var result = await scanner.Scan();
            var resultText = result.Text;
            resultEntry.Text = resultText;

            // Deserialize Json
            var qrcodeJson = JsonSerializer.Deserialize<QrcodeFormat>(resultText);
            var qrcodeAccount = qrcodeJson.Account;
            var qrcodePrice = qrcodeJson.Price;
            var qrcodeType = qrcodeJson.Type;

            // Call transfer Function from Smart Contract
            var transferFunc = erc20Contract.GetFunction("transfer");
            var gas = await transferFunc.EstimateGasAsync(account.Address, null, null, qrcodeAccount, qrcodePrice);
            var transaction = await transferFunc.SendTransactionAndWaitForReceiptAsync(account.Address, gas, null, null, qrcodeAccount, qrcodePrice);

            // Waiting for node's synchronization on Smart Contract (not necessary) 
            Thread.Sleep(2000);

            var transferEvent = erc20Contract.GetEvent<TransferEvent>();
            var filter = transferEvent.CreateFilterInput(account.Address, qrcodeAccount, null, null);
            var transferEventUpdate = await transferEvent.GetAllChanges(filter);

            var indexEvent = transferEventUpdate.Count-1;
            BigInteger refNum = transferEventUpdate[indexEvent].Event.Ref;
            var refHex = "0x" + refNum.ToString("x");

            // MQTT Publish Message
            var publishMessage = new MqttApplicationMessageBuilder()
                .WithTopic("pi/coffee/qr")
                .WithPayload(account.Address + "," + refHex + "," + qrcodeType.ToString())
                .WithAtLeastOnceQoS()
                .Build();
            await mqttClient.PublishAsync(publishMessage, CancellationToken.None);



        }
    }
}
