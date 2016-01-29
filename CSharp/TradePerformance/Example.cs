using System.Threading;

namespace TradePerformance
{
    using System;
    using System.IO;
    using System.Reflection;
    using SoftFX.Extended;
    using SoftFX.Extended.Events;
    using SoftFX.Extended.Storage;

    abstract class Example : IDisposable
    {
        #region Construction

        protected Example(string address, string username, string password)
            : this(address, username, password, useFixProtocol: true)
        {
        }

        protected Example(string address, string username, string password, bool useFixProtocol)
        {
            Username = username;

            // Create folders
            EnsureDirectoriesCreated();

            // Create builder
            this.fbuilder = useFixProtocol ? CreateFeedFixConnectionStringBuilder(address, username, password, LogPath)
                                          : CreateFeedLrpConnectionStringBuilder(address, username, password, LogPath);
            this.tbuilder = useFixProtocol ? CreateTradeFixConnectionStringBuilder(address, username, password, LogPath)
                                          : CreateTradeLrpConnectionStringBuilder(address, username, password, LogPath);

            this.Feed = new DataFeed
            {
                SynchOperationTimeout = 60000
            };
            this.Trade = new DataTrade
            {
                SynchOperationTimeout = 180000
            };

            this.Storage = new DataFeedStorage(StoragePath, StorageProvider.Ntfs, this.Feed, true);

        }

        static ConnectionStringBuilder CreateFeedFixConnectionStringBuilder(string address, string username, string password, string logDirectory)
        {
            var result = new FixConnectionStringBuilder
            {
                SecureConnection = true,
                Port = 5003,
                //ExcludeMessagesFromLogs = "W"
                Address = address,
                FixLogDirectory = logDirectory,
                FixEventsFileName = string.Format("FIX_{0}.feed.events.log", username),
                FixMessagesFileName = string.Format("FIX_{0}.feed.messages.log", username),

                Username = username,
                Password = password
            };

            return result;
        }

        static ConnectionStringBuilder CreateFeedLrpConnectionStringBuilder(string address, string username, string password, string logDirectory)
        {
            var result = new LrpConnectionStringBuilder
            {
                Address = address,
                EnableQuotesLogging = true,
                EventsLogFileName = Path.Combine(logDirectory, string.Format("LRP_{0}.feed.events.log", username)),
                MessagesLogFileName = Path.Combine(logDirectory, string.Format("LRP_{0}.feed.messages.log", username)),

                Username = username,
                Password = password
            };

            return result;
        }

        static ConnectionStringBuilder CreateTradeFixConnectionStringBuilder(string address, string username, string password, string logDirectory)
        {
            var result = new FixConnectionStringBuilder
            {
                SecureConnection = true,
                Port = 5004,
                //ExcludeMessagesFromLogs = "W"
                Address = address,
                FixLogDirectory = logDirectory,
                FixEventsFileName = string.Format("FIX_{0}.trade.events.log", username),
                FixMessagesFileName = string.Format("FIX_{0}.trade.messages.log", username),

                Username = username,
                Password = password
            };

            return result;
        }

        static ConnectionStringBuilder CreateTradeLrpConnectionStringBuilder(string address, string username, string password, string logDirectory)
        {
            var result = new LrpConnectionStringBuilder
            {
                Address = address,
                EnableQuotesLogging = true,
                EventsLogFileName = Path.Combine(logDirectory, string.Format("LRP_{0}.trade.events.log", username)),
                MessagesLogFileName = Path.Combine(logDirectory, string.Format("LRP_{0}.trade.messages.log", username)),

                Username = username,
                Password = password
            };

            return result;
        }


        static void EnsureDirectoriesCreated()
        {
            if (!Directory.Exists(LogPath))
                Directory.CreateDirectory(LogPath);

            if (!Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);
        }

        #endregion

        #region Properties

        static string CommonPath
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                return assembly != null ? Path.GetDirectoryName(assembly.Location) : string.Empty;
            }
        }

        static string LogPath
        {
            get
            {
                return Path.Combine(CommonPath, "Logs");
            }
        }

        static string StoragePath
        {
            get
            {
                return Path.Combine(CommonPath, "Storage");
            }
        }

        protected static bool RunSilent { get { return Config.Default.RunSilent; } }

        protected string Username { get; }
        protected DataFeed Feed { get; private set; }
        protected DataFeedStorage Storage { get; private set; }
        protected DataTrade Trade { get; private set; }
        protected bool TradeEnabled { get; set; }

        #endregion

        #region Control Methods

        public void Run()
        {
            try
            {
                this.DoRun();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void DoRun()
        {
            this.Feed.Initialize(this.fbuilder.ToString());
            this.Feed.Logon += this.OnLogon;
            this.Feed.Logout += this.OnLogout;
            this.Feed.Notify += this.OnNotify;

            this.Feed.SessionInfo += this.OnSessionInfo;
            this.Feed.SymbolInfo += this.OnSymbolInfo;
            this.Feed.CurrencyInfo += this.OnCurrencyInfo;

            if (!this.Feed.Start(this.Feed.SynchOperationTimeout))
            {
                Console.ReadKey();
                throw new TimeoutException("Timeout of Feed logon waiting has been reached");
            }

            if (TradeEnabled)
            {
                this.Trade.Initialize(this.tbuilder.ToString());

                if (!this.Trade.Start(this.Trade.SynchOperationTimeout))
                {
                    Console.ReadKey();
                    throw new TimeoutException("Timeout of Trade logon waiting has been reached");
                }
            }

            this.RunExample();
        }

        #endregion

        #region Event Handlers

        void OnLogon(object sender, LogonEventArgs e)
        {
            if (!RunSilent) Console.WriteLine("{0} OnLogon(): {1}", Username, e);
        }

        void OnLogout(object sender, LogoutEventArgs e)
        {
            if (!RunSilent) Console.WriteLine("{0} OnLogout(): {1}", Username, e);
        }

        void OnSymbolInfo(object sender, SymbolInfoEventArgs e)
        {
        }

        void OnCurrencyInfo(object sender, CurrencyInfoEventArgs e)
        {
        }

        void OnSessionInfo(object sender, SessionInfoEventArgs e)
        {
        }

        void OnNotify(object sender, NotificationEventArgs e)
        {
            if (!RunSilent) Console.WriteLine("{0} OnNotify(): {1}", Username, e);
        }

        #endregion

        #region Abstract Methods

        protected abstract void RunExample();

        #endregion

        #region IDisposable Interface

        public void Dispose()
        {
            if (this.Feed != null)
            {
                this.Feed.Stop();
                this.Feed.Dispose();
                this.Feed = null;
            }

            if (this.Storage != null)
            {
                this.Storage.Dispose();
                this.Storage = null;
            }

            if (this.Trade != null)
            {
                if (this.Trade.IsStarted)
                    this.Trade.Stop();

                this.Trade.Dispose();
                this.Trade = null;
            }
        }

        #endregion

        #region Members

        readonly ConnectionStringBuilder fbuilder;
        readonly ConnectionStringBuilder tbuilder;

        #endregion
    }
}
