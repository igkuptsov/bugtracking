using BugTracking.Controllers;
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
    public class ProjectControllerTest
    {
        public ProjectControllerTest()
        {
        }

        public IEnumerable<ProjectItem> InitProjectItems()
        {
            return Enumerable.Range(1, 10)
                .Select(i => new ProjectItem
                {
                    ProjectId = i,
                    ProjectName = $"Name{i}",
                    ProjectDescription = $"Description{i}",
                    ProjectDateCreated = new DateTime(2012, 12, 22).AddDays(i),
                    ProjectDateUpdated = new DateTime(2012, 12, 22).AddDays(i),
                });
        }

        public BugTrackingContext InitContext(IEnumerable<ProjectItem> projectItems, string dbName)
        {
            var builder = new DbContextOptionsBuilder<BugTrackingContext>()
                .UseInMemoryDatabase(dbName);

            var context = new BugTrackingContext(builder.Options);
            context.ProjectItems.AddRange(projectItems);
            int changed = context.SaveChanges();

            return context;
        }

        public ProjectController InitProjectController(BugTrackingContext context)
        {
            var controller = new ProjectController(context);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            return controller;
        }

        [Fact]
        public void TestGetProjectItems_GetAll()
        {

            var fakeProjectItems = InitProjectItems();
            using (var context = InitContext(fakeProjectItems, nameof(TestGetProjectItems_GetAll)))
            {

                var controller = InitProjectController(context);
                var projectItems = controller.GetProjectItems();

                Assert.Equal(fakeProjectItems.Count(), projectItems.Count());
                Assert.Equal(fakeProjectItems.Count(), int.Parse(controller.Response.Headers[ProjectController.TotalCountKey]));
                foreach (var item in fakeProjectItems)
                {
                    Assert.Contains(projectItems, i => i.ProjectId == item.ProjectId);
                }
            }
        }

        [Fact]
        public void TestGetProjectItems_SkipTake()
        {
            var fakeProjectItems = InitProjectItems();
            using (var context = InitContext(fakeProjectItems, nameof(TestGetProjectItems_SkipTake)))
            {
                var controller = InitProjectController(context);

                var skip = 5;
                var take = 2;
                var projectItemsExpected = fakeProjectItems.Skip(skip).Take(take);
                var projectItems = controller.GetProjectItems(skip, take);

                Assert.Equal(projectItemsExpected.Count(), projectItems.Count());
                Assert.Equal(fakeProjectItems.Count(), int.Parse(controller.Response.Headers[ProjectController.TotalCountKey]));
                foreach (var item in projectItemsExpected)
                {
                    Assert.Contains(projectItems, i => i.ProjectId == item.ProjectId);
                }
            }
        }

        [Fact]
        public void TestGetProjectItem()
        {
            var fakeProjectItems = InitProjectItems();
            using (var context = InitContext(fakeProjectItems, nameof(TestGetProjectItem)))
            {
                var controller = InitProjectController(context);
                var id = 4;

                var projectItemsExpected = fakeProjectItems.FirstOrDefault(i => i.ProjectId == 4);
                var actionResult = controller.GetProjectItem(id);
                Assert.True(actionResult.Result is OkObjectResult);

                var result = (ObjectResult)actionResult.Result;
                Assert.True(result.Value is ProjectItem);

                var projectItem = (ProjectItem)result.Value;
                Assert.Equal(projectItemsExpected.ProjectId, projectItem.ProjectId);
                Assert.Equal(projectItemsExpected.ProjectName, projectItem.ProjectName);
                Assert.Equal(projectItemsExpected.ProjectDescription, projectItem.ProjectDescription);
                Assert.Equal(projectItemsExpected.ProjectDateCreated, projectItem.ProjectDateCreated);
                Assert.Equal(projectItemsExpected.ProjectDateUpdated, projectItem.ProjectDateUpdated);
            }
        }

        [Fact]
        public void TestGetProjectItem_NotFound()
        {
            var fakeProjectItems = InitProjectItems();
            using (var context = InitContext(fakeProjectItems, nameof(TestGetProjectItem_NotFound)))
            {
                var controller = InitProjectController(context);
                var actionResult = controller.GetProjectItem(20);
                Assert.True(actionResult.Result is NotFoundResult);
            }
        }

        [Fact]
        public void TestPutProjectItem_NotEqualsIdsReturnBadRequest()
        {
            using (var context = InitContext(new List<ProjectItem> { }, nameof(TestPutProjectItem_NotEqualsIdsReturnBadRequest)))
            {
                var controller = InitProjectController(context);
                var actionResult = controller.PutProjectItem(1, new ProjectItem { ProjectId = 2 });
                Assert.True(actionResult.Result is BadRequestResult);
            }
        }

        [Fact]
        public void TestPutProjectItem_NotFoundResponse()
        {
            using (var context = InitContext(new List<ProjectItem> { }, nameof(TestPutProjectItem_NotFoundResponse)))
            {
                var controller = InitProjectController(context);
                var actionResult = controller.PutProjectItem(1, new ProjectItem { ProjectId = 1 });
                Assert.True(actionResult.Result is NotFoundResult);
            }
        }

        [Fact]
        public void TestPutProjectItem()
        {
            var id = 2;
            var fakeProjectItems = InitProjectItems();
            var beforePutProjectItem = fakeProjectItems.FirstOrDefault(i => i.ProjectId == id);
            var putProjectItem = new ProjectItem { ProjectId = id, ProjectName = "NewName", ProjectDescription = "NewDescription", ProjectDateCreated = new DateTime(2019, 01, 01) };

            using (var context = InitContext(fakeProjectItems, nameof(TestPutProjectItem)))
            {
                var controller = InitProjectController(context);
                var actionResult = controller.PutProjectItem(id, putProjectItem);

                Assert.True(actionResult.Result is NoContentResult);

                var afterPutProjectItem = context.ProjectItems.Find(id);

                Assert.NotNull(afterPutProjectItem);

                Assert.Equal(putProjectItem.ProjectId, afterPutProjectItem.ProjectId);
                Assert.Equal(putProjectItem.ProjectName, afterPutProjectItem.ProjectName);
                Assert.Equal(putProjectItem.ProjectDescription, afterPutProjectItem.ProjectDescription);
                Assert.Equal(beforePutProjectItem.ProjectDateCreated, afterPutProjectItem.ProjectDateCreated); //created date is unchangeable
                Assert.NotEqual(beforePutProjectItem.ProjectDateUpdated, afterPutProjectItem.ProjectDateUpdated); //updated date in refreshed
            }
        }

        [Fact]
        public void TestGetProjectItem_ModelStateInvalid()
        {
            var fakeProjectItems = InitProjectItems();
            using (var context = InitContext(fakeProjectItems, nameof(TestGetProjectItem_ModelStateInvalid)))
            {
                var controller = InitProjectController(context);
                controller.ModelState.AddModelError("fakeError", "fakeError");
                var result = controller.GetProjectItem(fakeProjectItems.First().ProjectId);

                Assert.True(result.Result is BadRequestObjectResult);
            }
        }

        [Fact]
        public void TestPutProjectItem_ModelStateInvalid()
        {
            var fakeProjectItems = InitProjectItems();
            using (var context = InitContext(fakeProjectItems, nameof(TestPutProjectItem_ModelStateInvalid)))
            {
                var controller = InitProjectController(context);
                controller.ModelState.AddModelError("fakeError", "fakeError");

                var fakeProjectItemsFirst = fakeProjectItems.First();
                var result = controller.PutProjectItem(fakeProjectItemsFirst.ProjectId, fakeProjectItemsFirst);

                Assert.True(result.Result is BadRequestObjectResult);
            }
        }

        [Fact]
        public void TestPostProjectItem_ModelStateInvalid()
        {
            using (var context = InitContext(new List<ProjectItem> { } , nameof(TestPostProjectItem_ModelStateInvalid)))
            {
                var controller = InitProjectController(context);
                controller.ModelState.AddModelError("fakeError", "fakeError");

                var result = controller.PostProjectItem(new ProjectItem { ProjectId = 1, ProjectName = "name", ProjectDescription = "description", ProjectDateCreated = DateTime.Now, ProjectDateUpdated = DateTime.Now });

                Assert.True(result.Result is BadRequestObjectResult);
            }
        }

        [Fact]
        public void TestPostProjectItem()
        {
            using (var context = InitContext(new List<ProjectItem> { }, nameof(TestPostProjectItem)))
            {
                var controller = InitProjectController(context);
                var postProjectItem = new ProjectItem { ProjectName = "name", ProjectDescription = "description", ProjectDateCreated = DateTime.Now.AddDays(1), ProjectDateUpdated = DateTime.Now.AddDays(2) };
                var actionResult = controller.PostProjectItem(new ProjectItem { ProjectId = 1, ProjectName = "name", ProjectDescription = "description", ProjectDateCreated = DateTime.Now.AddDays(1), ProjectDateUpdated = DateTime.Now.AddDays(1) });

                Assert.True(actionResult.Result is CreatedAtActionResult);

                var result = (CreatedAtActionResult)actionResult.Result;
                var resultProjectItem = (ProjectItem)result.Value;
                var afterPostProjectItem = context.ProjectItems.Find(resultProjectItem.ProjectId);

                Assert.Equal(postProjectItem.ProjectName, afterPostProjectItem.ProjectName);
                Assert.Equal(postProjectItem.ProjectDescription, afterPostProjectItem.ProjectDescription);
                Assert.True(postProjectItem.ProjectDateCreated > afterPostProjectItem.ProjectDateCreated); //check that dates correctly sets in controller
                Assert.True(postProjectItem.ProjectDateUpdated > afterPostProjectItem.ProjectDateUpdated);
                Assert.Equal(afterPostProjectItem.ProjectDateCreated, afterPostProjectItem.ProjectDateUpdated);
            }
        }

        [Fact]
        public void TestDeleteProjectItem_ModelStateInvalid()
        {
            var fakeProjectItems = InitProjectItems();
            using (var context = InitContext(fakeProjectItems, nameof(TestDeleteProjectItem_ModelStateInvalid)))
            {
                var controller = InitProjectController(context);
                controller.ModelState.AddModelError("fakeError", "fakeError");

                var result = controller.DeleteProjectItem(fakeProjectItems.First().ProjectId);

                Assert.True(result.Result is BadRequestObjectResult);
            }
        }

        [Fact]
        public void TestDeleteProjectItem_NotFound()
        {
            using (var context = InitContext(new List<ProjectItem> { }, nameof(TestDeleteProjectItem_NotFound)))
            {
                var controller = InitProjectController(context);
                var actionResult = controller.DeleteProjectItem(20);
                Assert.True(actionResult.Result is NotFoundResult);
            }
        }

        [Fact]
        public void TestDeleteProjectItem()
        {
            var fakeProjectItems = InitProjectItems();
            using (var context = InitContext(fakeProjectItems, nameof(TestDeleteProjectItem)))
            {
                var id = fakeProjectItems.First().ProjectId;
                var controller = InitProjectController(context);
                var actionResult = controller.DeleteProjectItem(id);

                Assert.True(actionResult.Result is OkObjectResult);

                var afterDeleteProjectItem = context.ProjectItems.Find(id);
                Assert.Null(afterDeleteProjectItem);

                var projectItmes = controller.GetProjectItems();

                Assert.Equal(fakeProjectItems.Count(), projectItmes.Count() + 1);
            }
        }   
    }
}
