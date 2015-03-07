﻿using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Filename to record quests
        const string questFile = "questlist.xml";

        // Local holding variables
        Tally tally = new Tally();
        Quests quests = new Quests();

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = quests;
            this.resultsWindow.DataContext = tally;
            this.partitionedVotes.DataContext = tally;
            this.partitionByBlock.DataContext = tally;
            this.partitionByLine.DataContext = tally;

            LoadQuests();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveQuests();
        }

        private async void tallyButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupEditQuestName();

            if (quests.CurrentQuest == null)
                return;

            tallyButton.IsEnabled = false;
            clearTallyCacheButton.IsEnabled = false;

            try
            {
                await tally.Run(quests.CurrentQuest.Name, quests.CurrentQuest.StartPost, quests.CurrentQuest.EndPost);
            }
            finally
            {
                tallyButton.IsEnabled = true;
                clearTallyCacheButton.IsEnabled = true;
            }
        }

        private void clearTallyCacheButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupEditQuestName();

            tallyButton.IsEnabled = false;
            clearTallyCacheButton.IsEnabled = false;

            try
            {
                tally.ClearPageCache();
            }
            finally
            {
                tallyButton.IsEnabled = true;
                clearTallyCacheButton.IsEnabled = true;
            }
        }

        private void copyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupEditQuestName();
            Clipboard.SetText(tally.TallyResults);
        }

        #region Adding and removing quests
        private void addQuestButton_Click(object sender, RoutedEventArgs e)
        {
            quests.AddToQuestList(new Quest());
            quests.SetCurrentQuestByName("New Entry");
            editQuestName.Visibility = Visibility.Visible;
            editQuestName.Focus();
            editQuestName.SelectAll();
        }

        private void removeQuestButton_Click(object sender, RoutedEventArgs e)
        {
            quests.RemoveCurrentQuest();
            CleanupEditQuestName();
        }

        /// <summary>
        /// Hitting enter will complete the entry.
        /// Hitting escape will cancel the entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editQuestName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CleanupEditQuestName();
            }
            else if (e.Key == Key.Escape)
            {
                quests.RemoveCurrentQuest();
                CleanupEditQuestName();
            }
        }

        private void CleanupEditQuestName()
        {
            if (editQuestName.Visibility == Visibility.Visible)
            {
                editQuestName.Visibility = Visibility.Hidden;
                quests.CurrentQuest.CleanName();
                var cqn = quests.CurrentQuest.Name;
                quests.Update();
                quests.SetCurrentQuestByName(cqn);
            }
        }
        #endregion


        #region Serialization
        private void LoadQuests()
        {
            string filepath = Path.Combine(System.Windows.Forms.Application.CommonAppDataPath, questFile);

            if (File.Exists(filepath))
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer sr = new XmlSerializer(typeof(Quests));
                    Quests qs = (Quests)sr.Deserialize(fs);
                    quests.SetCurrentQuestByName(qs.CurrentQuestName);
                }
            }
        }

        private void SaveQuests()
        {
            string filepath = Path.Combine(System.Windows.Forms.Application.CommonAppDataPath, questFile);

            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (TextWriter tw = new StreamWriter(fs))
                {
                    XmlSerializer sr = new XmlSerializer(typeof(Quests));
                    sr.Serialize(tw, quests);
                }
            }
        }

        #endregion

        private void post_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb != null)
                tb.SelectAll();
        }

        private void post_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = (sender as TextBox);

            if (tb != null)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    tb.Focus();
                }
            }
        }

    }
}
