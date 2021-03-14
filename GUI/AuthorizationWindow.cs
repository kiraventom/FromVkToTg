using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Authorization;
using VkNet;
using VkNet.Exception;

namespace GUI
{
	public partial class AuthorizationWindow : Window
	{
		public AuthorizationWindow()
		{
			InitializeComponent();
			this.AuthorizeBt.Click += AuthorizeBt_Click;			
		}

		public void AuthorizeBt_Click(object sender, RoutedEventArgs e)
		{
			string login = this.LoginTB.Text.Trim();
			string password = this.PasswordTB.Text.Trim();
			if (string.IsNullOrWhiteSpace(login))
			{
				MessageBox.Show("Логин не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (string.IsNullOrWhiteSpace(password))
			{
				MessageBox.Show("Пароль не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			try
			{
				Authorizer.Authorize(login, password);
			}
			catch (VkAuthorizationException ex)
			{
				MessageBox.Show(ex.Message, "Ошибка входа!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			this.Close();
		}
	}
}
