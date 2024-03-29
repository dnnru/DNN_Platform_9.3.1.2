#region Copyright
// 
// DotNetNukeŽ - http://www.dotnetnuke.com
// Copyright (c) 2002-2018
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using DotNetNuke.Common.Utilities;
using DotNetNuke.ComponentModel;

using Microsoft.VisualBasic;

using Globals = DotNetNuke.Common.Globals;

#endregion

namespace DotNetNuke.Services.Scheduling
{
    public class DNNScheduler : SchedulingProvider
    {

        #region Public Properties

        public override Dictionary<string, string> Settings
        {
            get
            {
                return ComponentFactory.GetComponentSettings<DNNScheduler>() as Dictionary<string, string>;
            }
        }

        #endregion

        #region Public Methods

        public override int AddSchedule(ScheduleItem scheduleItem)
        {
            //Remove item from queue
            Scheduler.CoreScheduler.RemoveFromScheduleQueue(scheduleItem);
            //save item
            scheduleItem.ScheduleID = SchedulingController.AddSchedule(scheduleItem.TypeFullName,
                                                                          scheduleItem.TimeLapse,
                                                                          scheduleItem.TimeLapseMeasurement,
                                                                          scheduleItem.RetryTimeLapse,
                                                                          scheduleItem.RetryTimeLapseMeasurement,
                                                                          scheduleItem.RetainHistoryNum,
                                                                          scheduleItem.AttachToEvent,
                                                                          scheduleItem.CatchUpEnabled,
                                                                          scheduleItem.Enabled,
                                                                          scheduleItem.ObjectDependencies,
                                                                          scheduleItem.Servers,
                                                                          scheduleItem.FriendlyName,
                                                                          scheduleItem.ScheduleStartDate);
            //Add schedule to queue
            RunScheduleItemNow(scheduleItem);

            //Return Id
            return scheduleItem.ScheduleID;
        }

        public override void AddScheduleItemSetting(int scheduleID, string name, string value)
        {
            SchedulingController.AddScheduleItemSetting(scheduleID, name, value);
        }

        public override void DeleteSchedule(ScheduleItem scheduleItem)
        {
            SchedulingController.DeleteSchedule(scheduleItem.ScheduleID);
            Scheduler.CoreScheduler.RemoveFromScheduleQueue(scheduleItem);
            DataCache.RemoveCache("ScheduleLastPolled");
        }

        public override void ExecuteTasks()
        {
            if (Enabled)
            {
                Scheduler.CoreScheduler.InitializeThreadPool(Debug, MaxThreads);
                Scheduler.CoreScheduler.KeepRunning = true;
                Scheduler.CoreScheduler.KeepThreadAlive = false;
                Scheduler.CoreScheduler.Start();
            }
        }

        public override int GetActiveThreadCount()
        {
            return SchedulingController.GetActiveThreadCount();
        }

        public override int GetFreeThreadCount()
        {
            return SchedulingController.GetFreeThreadCount();
        }

        public override int GetMaxThreadCount()
        {
            return SchedulingController.GetMaxThreadCount();
        }

        public override ScheduleItem GetNextScheduledTask(string server)
        {
            return SchedulingController.GetNextScheduledTask(server);
        }

        public override ArrayList GetSchedule()
        {
            return new ArrayList(SchedulingController.GetSchedule().ToArray());
        }

        public override ArrayList GetSchedule(string server)
        {
            return new ArrayList(SchedulingController.GetSchedule(server).ToArray());
        }

        public override ScheduleItem GetSchedule(int scheduleID)
        {
            return SchedulingController.GetSchedule(scheduleID);
        }

        public override ScheduleItem GetSchedule(string typeFullName, string server)
        {
            return SchedulingController.GetSchedule(typeFullName, server);
        }

        public override ArrayList GetScheduleHistory(int scheduleID)
        {
            return new ArrayList(SchedulingController.GetScheduleHistory(scheduleID).ToArray());
        }

        public override Hashtable GetScheduleItemSettings(int scheduleID)
        {
            return SchedulingController.GetScheduleItemSettings(scheduleID);
        }

        public override Collection GetScheduleProcessing()
        {
            return SchedulingController.GetScheduleProcessing();
        }

        public override Collection GetScheduleQueue()
        {
            return SchedulingController.GetScheduleQueue();
        }

        public override ScheduleStatus GetScheduleStatus()
        {
            return SchedulingController.GetScheduleStatus();
        }

        public override void Halt(string sourceOfHalt)
        {
            Scheduler.CoreScheduler.InitializeThreadPool(Debug, MaxThreads);
            Scheduler.CoreScheduler.Halt(sourceOfHalt);
            Scheduler.CoreScheduler.KeepRunning = false;
        }

        public override void PurgeScheduleHistory()
        {
            Scheduler.CoreScheduler.InitializeThreadPool(false, MaxThreads);
            Scheduler.CoreScheduler.PurgeScheduleHistory();
        }

        public override void ReStart(string sourceOfRestart)
        {
            Halt(sourceOfRestart);
            StartAndWaitForResponse();
        }

        public override void RunEventSchedule(EventName eventName)
        {
            if (Enabled)
            {
                Scheduler.CoreScheduler.InitializeThreadPool(Debug, MaxThreads);
                Scheduler.CoreScheduler.RunEventSchedule(eventName);
            }
        }

        public override void RunScheduleItemNow(ScheduleItem scheduleItem, bool runNow)
        {
            //Remove item from queue
            Scheduler.CoreScheduler.RemoveFromScheduleQueue(scheduleItem);
            var scheduleHistoryItem = new ScheduleHistoryItem(scheduleItem) { NextStart = runNow ? DateTime.Now : (scheduleItem.ScheduleStartDate != Null.NullDate ? scheduleItem.ScheduleStartDate : DateTime.Now) };

            if (scheduleHistoryItem.TimeLapse != Null.NullInteger
                && scheduleHistoryItem.TimeLapseMeasurement != Null.NullString
                && scheduleHistoryItem.Enabled
                && SchedulingController.CanRunOnThisServer(scheduleItem.Servers))
            {
                scheduleHistoryItem.ScheduleSource = ScheduleSource.STARTED_FROM_SCHEDULE_CHANGE;
                Scheduler.CoreScheduler.AddToScheduleQueue(scheduleHistoryItem);
            }

            DataCache.RemoveCache("ScheduleLastPolled");
        }

        public override void RunScheduleItemNow(ScheduleItem scheduleItem)
        {
            RunScheduleItemNow(scheduleItem, false);
        }

        public override void Start()
        {
            if (Enabled)
            {
                Scheduler.CoreScheduler.InitializeThreadPool(Debug, MaxThreads);
                Scheduler.CoreScheduler.KeepRunning = true;
                Scheduler.CoreScheduler.KeepThreadAlive = true;
                Scheduler.CoreScheduler.Start();
            }
        }

        public override void StartAndWaitForResponse()
        {
            if (Enabled)
            {
                var newThread = new Thread(Start) {IsBackground = true};
                newThread.Start();

                //wait for up to 30 seconds for thread
                //to start up
                for (int i = 0; i <= 30; i++)
                {
                    if (GetScheduleStatus() != ScheduleStatus.STOPPED)
                    {
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }
        }

        public override void UpdateScheduleWithoutExecution(ScheduleItem scheduleItem)
        {
            SchedulingController.UpdateSchedule(scheduleItem);
        }

        public override void UpdateSchedule(ScheduleItem scheduleItem)
        {
            //Remove item from queue
            Scheduler.CoreScheduler.RemoveFromScheduleQueue(scheduleItem);
            //save item
            SchedulingController.UpdateSchedule(scheduleItem);
            //Update items that are already scheduled
            var futureHistory = GetScheduleHistory(scheduleItem.ScheduleID).Cast<ScheduleHistoryItem>().Where(h => h.NextStart > DateTime.Now);

            var scheduleItemStart = scheduleItem.ScheduleStartDate > DateTime.Now
                                        ? scheduleItem.ScheduleStartDate
                                        : scheduleItem.NextStart;
            foreach (var scheduleHistoryItem in futureHistory)
            {
                scheduleHistoryItem.NextStart = scheduleItemStart;
                SchedulingController.UpdateScheduleHistory(scheduleHistoryItem);
            }


            //Add schedule to queue
            RunScheduleItemNow(scheduleItem);
        }

        //DNN-5001 Possibility to stop already running tasks
        public override void RemoveFromScheduleInProgress(ScheduleItem scheduleItem)
        {
            //get ScheduleHistoryItem of the running task
            var runningscheduleHistoryItem = GetScheduleHistory(scheduleItem.ScheduleID).Cast<ScheduleHistoryItem>().ElementAtOrDefault(0);
            Scheduler.CoreScheduler.StopScheduleInProgress(scheduleItem, runningscheduleHistoryItem);
        }

        #endregion
    }
}