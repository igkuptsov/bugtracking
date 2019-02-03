using BugTracking.Controllers;
using BugTracking.Enums;
using BugTracking.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace UnitTestProject
{
    public class TaskControllerTest
    {
        public TaskControllerTest()
        {
        }

        public IEnumerable<ProjectItem> InitProjectItems()
        {
            return Enumerable.Range(1, 2)
                .Select(i => new ProjectItem
                {
                    ProjectId = i,
                    ProjectName = $"Name{i}",
                    ProjectDescription = $"Description{i}",
                    ProjectDateCreated = new DateTime(2012, 12, 22).AddDays(i),
                    ProjectDateUpdated = new DateTime(2012, 12, 22).AddDays(i),
                });
        }

        public IEnumerable<TaskItem> InitTaskItems(IEnumerable<ProjectItem> projectItems, TaskStatus? taskStatus = null)
        {
            var taskId = 1;
            foreach (var projectItem in projectItems)
            {
                for (int i = 0; i < 10; i++)
                {
                    yield return new TaskItem
                    {
                        TaskId = taskId,
                        TaskName = $"Name{taskId}",
                        TaskDescription = $"Description{taskId}",
                        TaskPriority = taskId,
                        TaskStatus = taskStatus ?? TaskStatus.New,
                        TaskDateCreated = new DateTime(2012, 12, 22).AddDays(taskId),
                        TaskDateUpdated = new DateTime(2012, 12, 22).AddDays(taskId),
                        ProjectId = projectItem.ProjectId,
                    };

                    taskId++;
                }
            }
        }

        public BugTrackingContext InitContext(IEnumerable<ProjectItem> projectItems, IEnumerable<TaskItem> taskItems, string dbName)
        {
            var builder = new DbContextOptionsBuilder<BugTrackingContext>()
                .UseInMemoryDatabase(dbName);

            var context = new BugTrackingContext(builder.Options);
            context.ProjectItems.AddRange(projectItems);
            context.TaskItem.AddRange(taskItems);

            int changed = context.SaveChanges();

            return context;
        }

        public TaskController InitTaskController(BugTrackingContext context)
        {
            var controller = new TaskController(context);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            return controller;
        }


        [Fact]
        public void TestGetTaskItems_GetAll()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            var projectId = fakeProjectItems.First().ProjectId;
            var expectedTaskItems = fakeTaskItems.Where(i => i.ProjectId == projectId);

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_GetAll)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId, null);

                Assert.Equal(expectedTaskItems.Count(), taskItems.Count());
                Assert.Equal(expectedTaskItems.Count(), int.Parse(controller.Response.Headers[TaskController.TotalCountKey]));
                foreach (var item in expectedTaskItems)
                {
                    Assert.Contains(taskItems, i => i.TaskId == item.TaskId);
                }
            }
        }

        [Fact]
        public void TestGetTaskItems_SkipTake()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);

            var projectId = fakeProjectItems.First().ProjectId;
            var skip = 5;
            var take = 2;

            var expectedTaskItems = fakeTaskItems.Where(i => i.ProjectId == projectId).OrderBy(i => i.TaskDateCreated).Skip(skip).Take(take);

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_SkipTake)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId: projectId, skip: skip, take: take);

                Assert.Equal(expectedTaskItems.Count(), taskItems.Count());
                Assert.Equal(fakeTaskItems.Where(i => i.ProjectId == projectId).Count(), int.Parse(controller.Response.Headers[TaskController.TotalCountKey]));
                foreach (var item in expectedTaskItems)
                {
                    Assert.Contains(taskItems, i => i.TaskId == item.TaskId);
                }
            }
        }

        [Fact]
        public void TestGetTaskItems_PriorityFilter()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);

            var projectId = fakeProjectItems.First().ProjectId;
            var priority = 5;

            var expectedTaskItems = fakeTaskItems.Where(i => i.ProjectId == projectId && i.TaskPriority == priority);

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_PriorityFilter)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId: projectId, priorityFilter: priority);

                Assert.Equal(expectedTaskItems.Count(), taskItems.Count());
                Assert.Equal(expectedTaskItems.Count(), int.Parse(controller.Response.Headers[TaskController.TotalCountKey])); //X-Total-Count should show filtered count
                foreach (var item in expectedTaskItems)
                {
                    Assert.Contains(taskItems, i => i.TaskId == item.TaskId);
                }
            }
        }

        [Fact]
        public void TestGetTaskItems_CreatedDateFilter()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);

            var projectId = fakeProjectItems.First().ProjectId;
            var createdFrom = new DateTime(2012, 12, 22);
            var createdTo = new DateTime(2012, 12, 26);

            var expectedTaskItems = fakeTaskItems.Where(i => i.ProjectId == projectId && i.TaskDateCreated >= createdFrom && i.TaskDateCreated < createdTo.Date.AddDays(1));

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_CreatedDateFilter)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId: projectId, createdFrom: createdFrom, createdTo: createdTo);

                Assert.Equal(expectedTaskItems.Count(), taskItems.Count());
                Assert.Equal(expectedTaskItems.Count(), int.Parse(controller.Response.Headers[TaskController.TotalCountKey])); //X-Total-Count should show filtered count
                foreach (var item in expectedTaskItems)
                {
                    Assert.Contains(taskItems, i => i.TaskId == item.TaskId);
                }
            }
        }

        [Fact]
        public void TestGetTaskItems_SortByTaskDateCreatedAsc()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            var projectId = fakeProjectItems.First().ProjectId;

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_SortByTaskDateCreatedAsc)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId: projectId, sortField: nameof(TaskItem.TaskDateCreated), sortDirection: 0);
                var taskItemsSorted = taskItems.OrderBy(i => i.TaskDateCreated);

                Assert.True(taskItemsSorted.SequenceEqual(taskItems));
            }
        }

        [Fact]
        public void TestGetTaskItems_SortByTaskDateCreatedDesc()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            var projectId = fakeProjectItems.First().ProjectId;

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_SortByTaskDateCreatedDesc)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId: projectId, sortField: nameof(TaskItem.TaskDateCreated), sortDirection: 1);
                var taskItemsSorted = taskItems.OrderByDescending(i => i.TaskDateCreated);

                Assert.True(taskItemsSorted.SequenceEqual(taskItems));
            }
        }

        [Fact]
        public void TestGetTaskItems_SortByTaskPriorityAsc()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            var projectId = fakeProjectItems.First().ProjectId;

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_SortByTaskPriorityAsc)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId: projectId, sortField: nameof(TaskItem.TaskPriority), sortDirection: 0);
                var taskItemsSorted = taskItems.OrderBy(i => i.TaskPriority);

                Assert.True(taskItemsSorted.SequenceEqual(taskItems));
            }
        }

        [Fact]
        public void TestGetTaskItems_SortByTaskPriorityDesc()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            var projectId = fakeProjectItems.First().ProjectId;

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_SortByTaskPriorityDesc)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId: projectId, sortField: nameof(TaskItem.TaskPriority), sortDirection: 1);
                var taskItemsSorted = taskItems.OrderByDescending(i => i.TaskPriority);

                Assert.True(taskItemsSorted.SequenceEqual(taskItems));
            }
        }

        [Fact]
        public void TestGetTaskItems_DefaultSortAsc()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            var projectId = fakeProjectItems.First().ProjectId;

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_DefaultSortAsc)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId: projectId, sortField: null, sortDirection: 0);
                var taskItemsSorted = taskItems.OrderBy(i => i.TaskDateCreated);

                Assert.True(taskItemsSorted.SequenceEqual(taskItems));
            }
        }

        [Fact]
        public void TestGetTaskItems_DefaultSortDesc()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            var projectId = fakeProjectItems.First().ProjectId;

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItems_DefaultSortDesc)))
            {
                var controller = InitTaskController(context);
                var taskItems = controller.GetTaskItem(projectId: projectId, sortField: null, sortDirection: 1);
                var taskItemsSorted = taskItems.OrderByDescending(i => i.TaskDateCreated);

                Assert.True(taskItemsSorted.SequenceEqual(taskItems));
            }
        }


        [Fact]
        public void TestGetTaskItem()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            var taskItemExpected = fakeTaskItems.First();

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItem)))
            {
                var controller = InitTaskController(context);

                var actionResult = controller.GetTaskItem(taskItemExpected.TaskId);
                Assert.True(actionResult.Result is OkObjectResult);

                var result = (ObjectResult)actionResult.Result;
                Assert.True(result.Value is TaskItem);

                var taskItem = (TaskItem)result.Value;
                Assert.Equal(taskItemExpected.TaskId, taskItem.TaskId);
                Assert.Equal(taskItemExpected.TaskName, taskItem.TaskName);
                Assert.Equal(taskItemExpected.TaskDescription, taskItem.TaskDescription);
                Assert.Equal(taskItemExpected.TaskPriority, taskItem.TaskPriority);
                Assert.Equal(taskItemExpected.TaskStatus, taskItem.TaskStatus);
                Assert.Equal(taskItemExpected.TaskDateCreated, taskItem.TaskDateCreated);
                Assert.Equal(taskItemExpected.TaskDateUpdated, taskItem.TaskDateUpdated);
            }
        }

        [Fact]
        public void TestGetTaskItem_NotFound()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItem_NotFound)))
            {
                var controller = InitTaskController(context);
                var actionResult = controller.GetTaskItem(100);
                Assert.True(actionResult.Result is NotFoundResult);
            }
        }

        [Fact]
        public void TestGetTaskItem_ModelStateInvalid()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestGetTaskItem_ModelStateInvalid)))
            {
                var controller = InitTaskController(context);
                controller.ModelState.AddModelError("fakeError", "fakeError");
                var result = controller.GetTaskItem(fakeProjectItems.First().ProjectId);

                Assert.True(result.Result is BadRequestObjectResult);
            }
        }

        [Fact]
        public void TestPutTaskItem_ModelStateInvalid()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            var fakeTaskItemsFirest = fakeTaskItems.First();

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestPutTaskItem_ModelStateInvalid)))
            {
                var controller = InitTaskController(context);
                controller.ModelState.AddModelError("fakeError", "fakeError");

                var fakeProjectItemsFirst = fakeProjectItems.First();
                var result = controller.PutTaskItem(fakeTaskItemsFirest.TaskId, fakeTaskItemsFirest);

                Assert.True(result.Result is BadRequestObjectResult);
            }
        }

        [Fact]
        public void TestPostTaskItem_ModelStateInvalid()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);
            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestPostTaskItem_ModelStateInvalid)))
            {
                var controller = InitTaskController(context);
                controller.ModelState.AddModelError("fakeError", "fakeError");

                var result = controller.PostTaskItem(new TaskItem { TaskName = "name", TaskDescription = "description", TaskPriority = 1, TaskStatus = TaskStatus.New, TaskDateCreated = DateTime.Now, TaskDateUpdated = DateTime.Now });

                Assert.True(result.Result is BadRequestObjectResult);
            }
        }


        [Fact]
        public void TestPutTaskItem_NotEqualsIdsReturnBadRequest()
        {
            using (var context = InitContext(new List<ProjectItem> { }, new List<TaskItem> {}, nameof(TestPutTaskItem_NotEqualsIdsReturnBadRequest)))
            {
                var controller = InitTaskController(context);
                var actionResult = controller.PutTaskItem(1, new TaskItem { ProjectId = 2, TaskName = "name", TaskDescription = "description", TaskPriority = 1, TaskStatus = TaskStatus.New, TaskDateCreated = DateTime.Now, TaskDateUpdated = DateTime.Now });
                Assert.True(actionResult.Result is BadRequestResult);
            }
        }

        [Fact]
        public void TestPutTaskItem_NotFoundResponse()
        {
            var fakeProjectItems = InitProjectItems();
            using (var context = InitContext(fakeProjectItems, new List<TaskItem> { }, nameof(TestPutTaskItem_NotFoundResponse)))
            {
                var controller = InitTaskController(context);            
                var actionResult = controller.PutTaskItem(2, new TaskItem { TaskId = 2, ProjectId = 1, TaskName = "name", TaskDescription = "description", TaskPriority = 1, TaskStatus = TaskStatus.New, TaskDateCreated = DateTime.Now, TaskDateUpdated = DateTime.Now }); ;
                Assert.True(actionResult.Result is NotFoundResult);
            }
        }

        [Fact]
        public void TestPutTaskItem_ClosedTask()
        {        
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems, TaskStatus.Closed);
            var fakeTaskItem = fakeTaskItems.First();

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestPutTaskItem_ClosedTask)))
            {
                var controller = InitTaskController(context);
                var actionResult = controller.PutTaskItem(fakeTaskItem.TaskId, fakeTaskItem); ;
                Assert.True(actionResult.Result is BadRequestResult);
            }
        }

        [Fact]
        public void PutTaskItem()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);

            var beforePutTaskItem = fakeTaskItems.First();
            var taskId = beforePutTaskItem.TaskId;

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(PutTaskItem)))
            {
                var putTaskItem = new TaskItem { TaskId = taskId, TaskName = "New Task Name", TaskDescription = "New Task Description", TaskPriority = 100, TaskStatus = TaskStatus.InProgress, ProjectId = 2, TaskDateCreated = DateTime.Now.AddMonths(1), TaskDateUpdated = DateTime.Now.AddMonths(1) };
                var controller = InitTaskController(context);
                var actionResult = controller.PutTaskItem(taskId, putTaskItem);
                var afterPutTaskItem = context.TaskItem.Find(taskId);

                Assert.NotNull(afterPutTaskItem);

                Assert.Equal(putTaskItem.TaskId, afterPutTaskItem.TaskId);
                Assert.Equal(putTaskItem.TaskName, afterPutTaskItem.TaskName);
                Assert.Equal(putTaskItem.TaskDescription, afterPutTaskItem.TaskDescription);
                Assert.Equal(putTaskItem.TaskPriority, afterPutTaskItem.TaskPriority);
                Assert.Equal(putTaskItem.TaskStatus, afterPutTaskItem.TaskStatus);
                Assert.Equal(beforePutTaskItem.ProjectId, afterPutTaskItem.ProjectId); //project id is unchangeable
                Assert.Equal(beforePutTaskItem.TaskDateCreated, afterPutTaskItem.TaskDateCreated); //created date is unchangeable
                Assert.NotEqual(beforePutTaskItem.TaskDateUpdated, afterPutTaskItem.TaskDateUpdated); //updated date in refreshed
            }
        }

        [Fact]
        public void TestPostTaskItemTest()
        {
            var fakeProjectItems = InitProjectItems();

            using (var context = InitContext(fakeProjectItems, new List<TaskItem> { }, nameof(TestPostTaskItemTest)))
            {
                var tomorrow = DateTime.Now.AddDays(1);
                var postTaskItem = new TaskItem { TaskName = "New Task Name", TaskDescription = "New Task Description", TaskPriority = 100, TaskStatus = TaskStatus.InProgress, ProjectId = 1, TaskDateCreated = tomorrow, TaskDateUpdated = tomorrow };
                var controller = InitTaskController(context);
                var actionResult = controller.PostTaskItem(postTaskItem);

                Assert.True(actionResult.Result is CreatedAtActionResult);

                var result = (CreatedAtActionResult)actionResult.Result;
                var resultTaskItem = (TaskItem)result.Value;

                var afterPostTaskItem = context.TaskItem.Find(resultTaskItem.TaskId);

                Assert.NotNull(afterPostTaskItem);

                Assert.Equal(postTaskItem.TaskId, afterPostTaskItem.TaskId);
                Assert.Equal(postTaskItem.TaskName, afterPostTaskItem.TaskName);
                Assert.Equal(postTaskItem.TaskDescription, afterPostTaskItem.TaskDescription);
                Assert.Equal(postTaskItem.TaskPriority, afterPostTaskItem.TaskPriority);
                Assert.Equal(TaskStatus.New, afterPostTaskItem.TaskStatus);
                Assert.Equal(postTaskItem.ProjectId, afterPostTaskItem.ProjectId);

                Assert.True(tomorrow > afterPostTaskItem.TaskDateCreated); //check that dates correctly sets in controller
                Assert.True(tomorrow > afterPostTaskItem.TaskDateUpdated);
                Assert.Equal(afterPostTaskItem.TaskDateCreated, afterPostTaskItem.TaskDateUpdated);
            }
        }

        [Fact]
        public void TestDeleteTaskItem_ModelStateInvalid()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestDeleteTaskItem_ModelStateInvalid)))
            {
                var controller = InitTaskController(context);
                controller.ModelState.AddModelError("fakeError", "fakeError");

                var result = controller.DeleteTaskItem(fakeTaskItems.First().TaskId);

                Assert.True(result.Result is BadRequestObjectResult);
            }
        }

        [Fact]
        public void TestDeleteTaskItem_NotFound()
        {
            using (var context = InitContext(new List<ProjectItem> { }, new List<TaskItem> { }, nameof(TestDeleteTaskItem_NotFound)))
            {
                var controller = InitTaskController(context);
                var actionResult = controller.DeleteTaskItem(20);
                Assert.True(actionResult.Result is NotFoundResult);
            }
        }

        [Fact]
        public void TestDeleteTaskItem()
        {
            var fakeProjectItems = InitProjectItems();
            var fakeTaskItems = InitTaskItems(fakeProjectItems);

            var fakeTaskItem = fakeTaskItems.First();
            var taskId = fakeTaskItem.TaskId;
            var projectId = fakeTaskItem.ProjectId;

            using (var context = InitContext(fakeProjectItems, fakeTaskItems, nameof(TestDeleteTaskItem)))
            {                
                var controller = InitTaskController(context);
                var actionResult = controller.DeleteTaskItem(taskId);

                Assert.True(actionResult.Result is OkObjectResult);

                var afterDeleteTaskItem = context.TaskItem.Find(taskId);
                Assert.Null(afterDeleteTaskItem);

                var taskItems = controller.GetTaskItem(projectId:projectId);
                Assert.Equal(fakeTaskItems.Where(i => i.ProjectId == projectId).Count(), taskItems.Count() + 1);
            }
        }
    }
}
