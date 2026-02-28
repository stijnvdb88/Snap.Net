namespace Snap.Net.Avalonia.Contracts.Services;

public interface IFileService
{
    T? Read<T>(string folderPath, string fileName);

    void Save<T>(string folderPath, string fileName, T content);

    void Save(string folderPath, string fileName, byte[] content);

    void Delete(string folderPath, string fileName);
}
