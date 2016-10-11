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

namespace MotoTrak
{
    /// <summary>
    /// Interaction logic for SessionNotesView.xaml
    /// </summary>
    public partial class SessionNotesView : UserControl
    {
        #region Variables and methods for the CloseNotesEvent

        public static readonly RoutedEvent CloseNotesEvent = EventManager.RegisterRoutedEvent("CloseSessionNotes",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SessionNotesView));

        public event RoutedEventHandler CloseSessionNotes
        {
            add { AddHandler(CloseNotesEvent, value); }
            remove { RemoveHandler(CloseNotesEvent, value); }
        }

        private void RaiseCloseNotesEvent()
        {
            RaiseEvent(new RoutedEventArgs(CloseNotesEvent));
        }

        #endregion

        public SessionNotesView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Close the view
            RaiseCloseNotesEvent();
        }
    }
}
