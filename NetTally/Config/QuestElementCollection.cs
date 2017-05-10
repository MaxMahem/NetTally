﻿using System;
using System.Configuration;

namespace NetTally
{
    /// <summary>
    /// Wrapper class for a collection of quest elements to be added to the user config file.
    /// </summary>
    public class QuestElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() => new QuestElement();

        protected override object GetElementKey(ConfigurationElement element)
        {
            if (element is QuestElement qe)
            {
                if (!string.IsNullOrEmpty(qe.ThreadName))
                    return qe.ThreadName;
                return qe.DisplayName;
            }

            throw new ArgumentException("ConfigurationElement is not a QuestElement", nameof(element));
        }

        public new QuestElement this[string name] => (QuestElement)BaseGet(name);

        public QuestElement this[int index]
        {
            get
            {
                return (QuestElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(IQuest quest)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var questElement = new QuestElement(quest.ThreadName, quest.DisplayName, quest.PostsPerPage, quest.StartPost, quest.EndPost,
                quest.CheckForLastThreadmark, quest.PartitionMode, quest.UseCustomThreadmarkFilters, quest.CustomThreadmarkFilters,
                quest.UseCustomUsernameFilters, quest.CustomUsernameFilters, quest.UseCustomPostFilters, quest.CustomPostFilters);

            BaseAdd(questElement, false);
        }

        public void Clear()
        {
            BaseClear();
        }
    }
}
