namespace Net;

using System;
using System.IO;

public class NetFile : IDisposable
{
    private bool disposedValue;

    public FileStream Stream { get; }
    public string FullPath => Stream.Name;
    public string Name => Path.GetFileNameWithoutExtension(FullPath);
    public string Extension => Path.GetExtension(FullPath);

    internal NetFile(FileStream stream) => Stream = stream;

    public void Delete()
    {
        Stream.Close();
        File.Delete(FullPath);
    }

    public NetFile CopyToPath(string NewPath, string Name = null, bool OverwriteFile = false)
    {
        bool? val = null;

        if (Directory.Exists(NewPath))
            val = true;
        else if (File.Exists(NewPath))
            val = false;

        if (val == null) throw new IOException($"No such directory \"{NewPath}\"");
        else if (val == true)
        {
            if (Name != null)
            {
                if (val == true)
                    NewPath += $@"\{Name}.{Extension}";
            }
            else
                NewPath += $@"\{this.Name}{Extension}";
        }

        if (OverwriteFile)
        {
            using (FileStream fs = new FileStream(NewPath, FileMode.Create))
            {
                Stream.CopyTo(fs);
                fs.Flush();
                return new NetFile(fs);
            }
        }
        else
        {
            using (FileStream fs = new FileStream(NewPath, FileMode.CreateNew))
            {
                Stream.CopyTo(fs);
                fs.Flush();
                return new NetFile(fs);
            }
        }
    }

    public NetFile MoveToPath(string NewPath, string Name = null, bool OverwriteFile = false)
    {
        var file = CopyToPath(NewPath, Name, OverwriteFile);
        Delete();
        return file;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            Stream.Dispose();
            disposedValue = true;
        }
    }

    ~NetFile()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static implicit operator FileStream(NetFile f) => f.Stream;
}
