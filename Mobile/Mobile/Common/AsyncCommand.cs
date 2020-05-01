using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices;
using Xamarin.Forms;

namespace SnapDotNet.Mobile.Common
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }
    public class AsyncCommand : IAsyncCommand
    {
        public event EventHandler CanExecuteChanged;

        private bool m_IsExecuting;
        private readonly Func<Task> m_Execute;
        private readonly Func<bool> m_CanExecute;
        private readonly Action<Exception> m_ErrorHandler;

        public AsyncCommand(
            Func<Task> execute,
            Func<bool> canExecute = null,
            Action<Exception> errorHandler = null)
        {
            m_Execute = execute;
            m_CanExecute = canExecute;
            m_ErrorHandler = errorHandler;
        }

        public bool CanExecute()
        {
            return !m_IsExecuting && (m_CanExecute?.Invoke() ?? true);
        }

        public async Task ExecuteAsync()
        {
            if (CanExecute())
            {
                try
                {
                    m_IsExecuting = true;
                    await m_Execute();
                }
                finally
                {
                    m_IsExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Explicit implementations
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteAsync().SafeFireAndForget(m_ErrorHandler);
        }
        #endregion
    }
}
