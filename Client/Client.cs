﻿using Microsoft.AspNet.SignalR.Client;
using System;
using System.Drawing;
using System.Net;
using System.Net.Cache;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Web.Security;
using System.Windows.Forms;
using RestSharp;

namespace Client
{
    public partial class Client : Form
    {
        private readonly IHubProxy _hub;
        private readonly CompositeDisposable _disposables;

        public Client()
        {
            InitializeComponent();

            var uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);

            var hubConnection = new HubConnection(Shared.Hub.Url);
            _hub = hubConnection.CreateHubProxy(Shared.Hub.Name);

            var startConnectionDisposable = Observable.FromAsync(() => hubConnection.Start())
                                                      .ObserveOn(uiScheduler)
                                                      .Subscribe(_ => { }, ex => Log(ex.Message));

            var stateChangesObservable = Observable.FromEvent<StateChange>(
                                                        h => hubConnection.StateChanged += h,
                                                        h => hubConnection.StateChanged -= h)
                                                    .StartWith(new StateChange(ConnectionState.Disconnected, ConnectionState.Connecting))
                                                    .ObserveOn(uiScheduler)
                                                    .Do(sc => Log($"Went from {sc.OldState} to {sc.NewState}"))
                                                    .Publish();

            var controlSignalingDisposable = stateChangesObservable
                                                .Subscribe(sc =>
                                                {
                                                    AttachBehaviors();
                                                    SetControls(sc);
                                                });

            var disconnectionDisposable = stateChangesObservable
                                            .Where(sc => sc.NewState == ConnectionState.Disconnected)
                                            .Sample(TimeSpan.FromSeconds(10))
                                            .Subscribe(_ => hubConnection.Start());

            var hotDisposable = stateChangesObservable.Connect();

            _disposables = new CompositeDisposable
            {
                startConnectionDisposable,
                controlSignalingDisposable,
                disconnectionDisposable,
                hotDisposable
            };
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _disposables?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Log(string message)
        {
            lstStatus.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        private void AttachBehaviors()
        {
            _hub.On(nameof(Shared.IClient.HandleMessageFromServer),
                x => { txtOutput.BeginInvoke((Action)(() => txtOutput.Text = x)); });
        }

        private void SetControls(StateChange sc)
        {
            var enabled = sc.NewState == ConnectionState.Connected;
            var visible = sc.NewState == ConnectionState.Connected;

            grpLogin.Visible = visible;
           // grpInput.Visible = visible;
          //  grpOutput.Visible = visible;

            btnSend.Enabled = enabled;
            txtInput.Enabled = enabled;

            lstStatus.BackColor = enabled ? Color.MediumSeaGreen : Color.Firebrick;
        }

        private void btnSendClick(object sender, EventArgs e)
        {
            _hub.Invoke(nameof(Shared.IHub.HandleMessageFromCaller), txtInput.Text);
        }

        private void btnLoginClick(object sender, EventArgs e)
        {
            //_hub.Invoke(nameof(Shared.IHub.HandleLoginFromCaller), txtUser.Text, txtPass.Text);


            AuthenticateUser(txtUser.Text, txtPass.Text);
        }

        private static bool AuthenticateUser(string user, string password)
        {
            var client = new RestClient($"{Shared.Hub.Url}/token");
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", "grant_type=client_credentials&client_id=abc&client_secret=123", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return true;
        }
    }
}
