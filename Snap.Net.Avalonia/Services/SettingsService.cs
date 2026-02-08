using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Snap.Net.Avalonia.Contracts.Services;

namespace Snap.Net.Avalonia.Services;

public class SettingsService : ISettingsService
{
    private readonly IFileService m_FileService;
    private Dictionary<string, object?>? m_Settings = new Dictionary<string, object?>();
    private readonly string m_ProjectLocalAppData;
    private const string SETTINGS_FILE_NAME = "Settings.json";
    
    public SettingsService(IFileService fileService)
    {
        m_FileService = fileService;
        if (Application.Current != null && string.IsNullOrEmpty(Application.Current.Name) == false)
        {
            m_ProjectLocalAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.Current.Name);    
        }
        else
        {
            m_ProjectLocalAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MGSDeploy");
        }
        _Load();
    }
    
    public void Save()
    {
        m_FileService.Save(m_ProjectLocalAppData, SETTINGS_FILE_NAME, m_Settings);
    }

    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (m_Settings == null || m_Settings.TryGetValue(key, out object? result) == false)
        {
            return defaultValue;
        }
        
        if (result != null)
        {
            if (typeof(T).BaseType == typeof(System.Enum))
            {
                int value = (int)Enum.Parse(typeof(T), result.ToString() ?? string.Empty);
                return (T)Enum.ToObject(typeof(T), value);
            }
            return (T)Convert.ChangeType(result, typeof(T));
        }
        return defaultValue;
    }

    public void Set<T>(string key, T? value, bool save = true)
    {
        if (m_Settings == null)
        {
            throw new Exception("Settings not loaded");
        }
        m_Settings[key] = value;
        if (save)
        {
            Save();
        }
    }
    
    private void _Load()
    {
        m_Settings = m_FileService.Read<Dictionary<string, object?>>(m_ProjectLocalAppData, SETTINGS_FILE_NAME);
        if (m_Settings == null)
        {
            // defaults:
            m_Settings = new Dictionary<string, object?>();

            Save();
        }
    }
}