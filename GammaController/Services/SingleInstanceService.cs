using System.IO;
using System.IO.Pipes;

namespace GammaController.Services;

public class SingleInstanceService : IDisposable
{
    private const string MutexName = "GammaController_SingleInstance_Mutex";
    private const string PipeName = "GammaController_SingleInstance_Pipe";
    private const string ShowWindowCommand = "SHOW";

    private Mutex? _mutex;
    private CancellationTokenSource? _pipeServerCts;
    private bool _disposed;

    public event EventHandler? ShowWindowRequested;

    public bool TryAcquireLock()
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        
        if (!createdNew)
        {
            // Another instance exists, signal it to show
            SendShowCommand();
            _mutex.Dispose();
            _mutex = null;
            return false;
        }
        
        return true;
    }

    public void StartListening()
    {
        _pipeServerCts = new CancellationTokenSource();
        _ = ListenForCommandsAsync(_pipeServerCts.Token);
    }

    private async Task ListenForCommandsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipeServer.WaitForConnectionAsync(cancellationToken);
                
                using var reader = new StreamReader(pipeServer);
                var command = await reader.ReadLineAsync(cancellationToken);
                
                if (command == ShowWindowCommand)
                {
                    ShowWindowRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pipe server error: {ex.Message}");
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    private static void SendShowCommand()
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipeClient.Connect(1000); // 1 second timeout
            
            using var writer = new StreamWriter(pipeClient) { AutoFlush = true };
            writer.WriteLine(ShowWindowCommand);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send show command: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _pipeServerCts?.Cancel();
        _pipeServerCts?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}

