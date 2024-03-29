﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SnapDotNet.Mobile.Common
{
    public enum EBroadcastMode
    {
        Media,
        Microphone
    }

    public static class SnapSettings
    {
        public static string Server
        {
            get => Get<string>("Server");

            set => Set("Server", value);
        }

        public static int ControlPort
        {
            get => Get<int>("ControlPort", 1705);

            set => Set<int>("ControlPort", value);
        }

        public static int PlayerPort
        {
            get => Get<int>("PlayerPort", 1704);

            set => Set<int>("PlayerPort", value);
        }

        public static int BroadcastPort
        {
            get => Get<int>("BroadcastPort", 4953);

            set => Set<int>("BroadcastPort", value);
        }

        public static EBroadcastMode BroadcastMode
        {
            get => (EBroadcastMode)Get<int>("BroadcastMode", (int)EBroadcastMode.Media);

            set => Set<int>("BroadcastMode", (int)value);
        }

        public static void Set<T>(string key, T value)
        {
            if (Xamarin.Forms.Application.Current.Properties.ContainsKey(key) == false)
            {
                Xamarin.Forms.Application.Current.Properties.Add(key, value);
            }
            else
            {
                Xamarin.Forms.Application.Current.Properties[key] = value;
            }

            Xamarin.Forms.Application.Current.SavePropertiesAsync().ConfigureAwait(false);
        }

        public static T Get<T>(string key, T defaultValue = default(T))
        {
            if (Xamarin.Forms.Application.Current.Properties.ContainsKey(key) == true)
            {
                return (T)Xamarin.Forms.Application.Current.Properties[key];
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
