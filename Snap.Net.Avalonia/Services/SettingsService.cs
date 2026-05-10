using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia;
using Snap.Net.Avalonia.Contracts.Services;

namespace Snap.Net.Avalonia.Services;

public class SettingsService : ISettingsService
{
    private readonly IFileService m_FileService;
    private Dictionary<string, string>? m_Settings = new Dictionary<string, string>();
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
            m_ProjectLocalAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Snap.Net.Avalonia");
        }
        _Load();
    }
    
    public void Save()
    {
        m_FileService.Save(m_ProjectLocalAppData, SETTINGS_FILE_NAME, m_Settings);
    }

    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (m_Settings == null || m_Settings.TryGetValue(key, out string? result) == false)
        {
            return defaultValue;
        }

        if (string.IsNullOrEmpty(result))
        {
            return defaultValue;
        }
        
        if (typeof(T).BaseType == typeof(System.Enum))
        {
            int value = (int)Enum.Parse(typeof(T), result.ToString() ?? string.Empty);
            return (T)Enum.ToObject(typeof(T), value);
        }
        
        // in case object is unknown;
        try
        {
            return JsonSerializer.Deserialize<T>(result);
        }
        catch
        {
            return defaultValue;
        }
    }

    public void Set<T>(string key, T? value, bool save = true)
    {
        if (m_Settings == null)
        {
            throw new Exception("Settings not loaded");
        }
        m_Settings[key] = JsonSerializer.Serialize(value, typeof(T));
        if (save)
        {
            Save();
        }
    }
    
    private void _Load()
    {
        m_Settings = m_FileService.Read<Dictionary<string, string>>(m_ProjectLocalAppData, SETTINGS_FILE_NAME);
        if (m_Settings == null)
        {
            // defaults:
            m_Settings = new Dictionary<string, string>();
            Save();
        }
    }
}