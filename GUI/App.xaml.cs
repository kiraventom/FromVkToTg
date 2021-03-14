using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Authorization;

namespace GUI
{
	public partial class App : Application
	{
		public void Applicaton_Startup(object sender, StartupEventArgs e)
		{
			bool shouldClose = HandleAuthorization();
			if (shouldClose)
			{
				return;
			}


		}

		/// <summary>
		/// <returns>Если возвращает <see cref="true"/>, приложение должно закрыться</returns>
		/// </summary>
		private void HandleAuthorization()
		{
			var authResult = Authorizer.Authorize();
			switch (authResult)
			{
				case AuthorizationResult.OK:
					return;
					
					case AuthorizationResult.TokenNotFound:
					
				default:
			}


			var mbr = HandleTokenError();
			switch (mbr)
			{
				case MessageBoxResult.Yes:
					shouldBreak = true;
					break;

				case MessageBoxResult.No:
					continue;

				case MessageBoxResult.Cancel:
					this.Shutdown(); // антипаттерн, надо поправить в будущем
					break;
			}
			if (Authorizer.IsAuthorized)
			{
				return false;
			}

			AuthorizationWindow authWindow = new();
			authWindow.ShowDialog();

			return !Authorizer.IsAuthorized;
		}

		private MessageBoxResult HandleTokenError()
		{
			StringBuilder messageBuilder = new();
			messageBuilder.AppendLine("Не удалось войти через сохранённый токен!\n");
			messageBuilder.AppendLine("Нажмите Да, чтобы войти через логин и пароль;");
			messageBuilder.AppendLine("Нажмите Нет, чтобы попробовать снова;");
			messageBuilder.AppendLine("Нажмите Отмена, чтобы выйти.");
			var mbr = MessageBox.Show(messageBuilder.ToString(), "Ошибка!", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
			return mbr;
		}
	}
}
