﻿//-----------------------------------------------------------------------
// <copyright file="StorageAccountProperties.cs" company="madd0.com">
//     Copyright (c) 2012 Mauricio DIAZ ORLICH.
//     Code licensed under the MIT X11 license.
// </copyright>
// <author>Mauricio DIAZ ORLICH</author>
//-----------------------------------------------------------------------

namespace Madd0.AzureStorageDriver
{
    using System;
    using System.Xml.Linq;
    using LINQPad.Extensibility.DataContext;
    using Madd0.AzureStorageDriver.Properties;
    using Microsoft.WindowsAzure;

    /// <summary>
    /// Wrapper to expose typed properties over ConnectionInfo.DriverData.
    /// </summary>
    public class StorageAccountProperties
    {
        private readonly IConnectionInfo _connectionInfo;
        private readonly XElement _driverData;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageAccountProperties"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection info.</param>
        public StorageAccountProperties(IConnectionInfo connectionInfo)
        {
            this._connectionInfo = connectionInfo;
            this._driverData = connectionInfo.DriverData;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this connection should be remembered.
        /// </summary>
        /// <value><c>true</c> if this connection should be remembered; otherwise, <c>false</c>.</value>
        public bool Persist
        {
            get { return this._connectionInfo.Persist; }
            set { this._connectionInfo.Persist = value; }
        }

        /// <summary>
        /// Gets the display name of the connection.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (this.UseLocalStorage)
                {
                    return Resources.LocalStorageDisplayName;
                }
                else
                {
                    return this.AccountName;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether local storage is being used.
        /// </summary>
        /// <value><c>true</c> if local storage is used; otherwise, <c>false</c>.</value>
        public bool UseLocalStorage
        {
            get
            {
                return (bool?)this._driverData.Element("UseLocalStorage") ?? false;
            }

            set
            {
                this._driverData.SetElementValue("UseLocalStorage", value);

                if (value)
                {
                    this.ClearAccountNameAndKey();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use HTTPS.
        /// </summary>
        /// <value><c>true</c> if HTTPS is used; otherwise, <c>false</c>.</value>
        public bool UseHttps
        {
            get
            {
                return (bool?)this._driverData.Element("UseHttps") ?? false;
            }

            set
            {
                this._driverData.SetElementValue("UseHttps", value);
            }
        }

        public bool ChinaAzure
        {
            get
            {
                return (bool?)this._driverData.Element("ChinaAzure") ?? false;
            }

            set
            {
                this._driverData.SetElementValue("ChinaAzure", value);
            }
        }

        /// <summary>
        /// Gets or sets the name of the storage account.
        /// </summary>
        public string AccountName
        {
            get { return (string)this._driverData.Element("AccountName") ?? string.Empty; }
            set { this._driverData.SetElementValue("AccountName", value); }
        }

        /// <summary>
        /// Gets or sets the key for the storage account.
        /// </summary>
        public string AccountKey
        {
            get
            {
                var encryptedKey = (string)this._driverData.Element("AccountKey") ?? string.Empty;
                return this._connectionInfo.Decrypt(encryptedKey);
            }

            set
            {
                var encryptedValue = this._connectionInfo.Encrypt(value);
                this._driverData.SetElementValue("AccountKey", encryptedValue);
            }
        }

        /// <summary>
        /// Gets a <see cref="CloudStorageAccount"/> instace for the current connection.
        /// </summary>
        /// <returns>A <see cref="CloudStorageAccount"/> instace configured with the credentials
        /// of the current connection.</returns>
        public CloudStorageAccount GetStorageAccount()
        {
            if (this.UseLocalStorage)
            {
                return CloudStorageAccount.DevelopmentStorageAccount;
            }
            else
            {
                if (!ChinaAzure)
                    return new CloudStorageAccount(new StorageCredentialsAccountAndKey(this.AccountName, this.AccountKey), this.UseHttps);
                return new CloudStorageAccount(new StorageCredentialsAccountAndKey(this.AccountName, this.AccountKey)
                    , BuildChinaAzureUri(AccountName, UseHttps, 0)
                    , BuildChinaAzureUri(AccountName, UseHttps, 2)
                    , BuildChinaAzureUri(AccountName, UseHttps, 1));

            }
        }

        private Uri BuildChinaAzureUri(string accountName, bool useHttps, int type)
        {
            string typeStr;
            switch (type)
            {
                case 0:
                    typeStr = "blob";
                    break;
                case 1:
                    typeStr = "table";
                    break;
                case 2:
                    typeStr = "queue";
                    break;
                default:
                    typeStr = "blob";
                    break;
            }
            return new Uri(String.Format("http{0}://{1}.{2}.core.chinacloudapi.cn/", useHttps ? "s" : "", accountName, typeStr));
        }


        /// <summary>
        /// Clears the account name and key.
        /// </summary>
        /// <remarks>This method is called when local storage is used.</remarks>
        private void ClearAccountNameAndKey()
        {
            var accountName = this._driverData.Element("AccountName");
            var accountKey = this._driverData.Element("AccountKey");

            if (null != accountName)
            {
                accountName.Remove();
            }

            if (null != accountKey)
            {
                accountKey.Remove();
            }
        }
    }
}
