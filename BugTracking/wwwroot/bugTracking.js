const projectPageSize = 10;
const uriProject = "api/project";
let projects = null;
let projectsPage = 0;

const taskPageSize = 10;
const uriTask = "api/task";
let tasks = null;
let taskPage = 0;

$(document).ready(function () {
    getProjects();
    $(".datepicker").datepicker();
});

function addProject() {
    const item = {
        projectName: $("#add-project-name").val(),
        projectDescription: $("#add-project-description").val(),
    };

    $.ajax({
        type: "POST",
        accepts: "application/json",
        url: uriProject,
        contentType: "application/json",
        data: JSON.stringify(item),
        error: function (jqXHR, textStatus, errorThrown) {
            alert("Something went wrong!");
        },
        success: function (result) {
            getProjects();
            $("#add-project-name").val("");
            $("#add-project-description").val("");
        }
    });
}

function renderPager(continer, totalCount, currentPage, pageSize, getFunction) {
    let pageCount = totalCount === 0 ? 1 : Math.ceil(totalCount / pageSize);    

    if (currentPage > pageCount - 1 && pageCount !== 0) {
        currentPage = currentPage - 1;
        getFunction(currentPage);
    }

    const pager = continer;
    pager.empty();

    for (let i = 0; i < pageCount; i++) {
        let span = $("<span></span>").text(i + 1).attr('page', i).addClass('page-item');
        if (i === currentPage) {
            span.addClass('selected');
        }

        span.appendTo(pager);
    }

    $('.page-item').on("click", function () {
        let page = this.getAttribute('page');
        getFunction(parseInt(page));
    });
}

function getProjects(currentPage = null) {
    if (currentPage !== null) {
        projectsPage = currentPage;
    }

    let skip = projectsPage * projectPageSize;
    let take = projectPageSize;

    $.ajax({
        type: "GET",
        url: uriProject+'?skip=' + skip + '&take=' + take,
        cache: false,
        success: function (data, textStatus, request) {
            const tBody = $("#projects");

            $(tBody).empty();

            let projectCount = parseInt(request.getResponseHeader('x-total-count'));

            $.each(data, function (key, item) {
                const tr = $("<tr></tr>")
                    .append($("<td></td>").text(item.projectName))
                    .append($("<td></td>").text(item.projectDescription))
                    .append($("<td></td>").text(item.projectDateCreated))
                    .append($("<td></td>").text(item.projectDateUpdated))
                    .append(
                        $("<td></td>").append(
                            $("<button>Edit</button>").on("click", function () {
                                editProject(item.projectId);
                            })
                        )
                    )
                    .append(
                        $("<td></td>").append(
                            $("<button>Delete</button>").on("click", function () {
                                deleteProject(item.projectId);
                            })
                        )
                    )
                    .append(
                        $("<td></td>").append(
                            $("<button>Open</button>").on("click", function () {
                                clearFilter();
                                openProject(item.projectId);                            
                            })
                        )
                    );

                tr.appendTo(tBody);
            });

            projects = data;
            renderPager($('#projectsPager'), projectCount, projectsPage, projectPageSize, getProjects);
        }
    });
}

function deleteProject(projectId) {
    $.ajax({
        url: uriProject + "/" + projectId,
        type: "DELETE",
        error: function (jqXHR, textStatus, errorThrown) {
            alert("Something went wrong!");
        },
        success: function (result) {
            closeProjectInput();
            getProjects();
            if ($('#select-project-id').val() == projectId) {
                $("#tasks-area").hide();
            }
        }
    });
}

function editProject(projectId) {
    $.each(projects, function (key, item) {
        if (item.projectId === projectId) {
            $("#edit-project-id").val(item.projectId);
            $("#edit-project-name").val(item.projectName);
            $("#edit-project-description").val(item.projectDescription);
        }
    });
    $("#edit-project").show();
}


$(".edit-project-form").on("submit", function () {
    const item = {
        projectId: $("#edit-project-id").val(),
        projectName: $("#edit-project-name").val(),
        projectDescription: $("#edit-project-description").val()
    };

    $.ajax({
        url: uriProject + "/" + $("#edit-project-id").val(),
        type: "PUT",
        accepts: "application/json",
        contentType: "application/json",
        data: JSON.stringify(item),
        error: function (jqXHR, textStatus, errorThrown) {
            alert("Something went wrong!");
        },
        success: function (result) {
            getProjects();
        }
    });

    closeProjectInput();
    return false;
});

function closeProjectInput() {
    $("#edit-project").hide();
}

function getTaskStatusTitile(taskStatus) {
    switch (taskStatus) {
        case 0:
            return 'New';
        case 1:
            return 'In progress';
        case 2:
            return 'Closed';
        default:
            return 'unknown';
    }
}

function openProject(projectId) {
    $('#select-project-id').val(projectId);
    getTasks(0);
}

function getTasks(currentPage = null) {
    if (currentPage !== null) {
        taskPage = currentPage;
    }

    let sortField = $('#task-sort-field').val();
    let sortDirection = $('#task-sort-direction').val();
    let projectId = $('#select-project-id').val();
    let taskFilterDateCreatedFrom = $('#task-filter-date-created-from').val();
    let taskFilterDateCreatedTo = $('#task-filter-date-created-to').val();
    let taskFilterPriority = $('#task-filter-priority').val();

    let skip = taskPage * taskPageSize;
    let take = taskPageSize;

    $.ajax({
        type: "GET",
        url: uriTask
            + '?projectId=' + projectId
            + '&skip=' + skip
            + '&take=' + take
            + '&sortField=' + sortField
            + '&sortDirection=' + sortDirection
            + '&priorityFilter=' + taskFilterPriority
            + '&createdFrom=' + taskFilterDateCreatedFrom
            + '&createdTo=' + taskFilterDateCreatedTo
        ,
        cache: false,
        success: function (data, textStatus, request) {
            const tBody = $("#tasks");

            $(tBody).empty();

            let taskCount = parseInt(request.getResponseHeader('x-total-count'));

            $.each(data, function (key, item) {
                const tr = $("<tr></tr>")
                    .append($("<td></td>").text(item.taskName))
                    .append($("<td></td>").text(item.taskDescription))
                    .append($("<td></td>").text(item.taskPriority))
                    .append($("<td></td>").text(getTaskStatusTitile(item.taskStatus)))
                    .append($("<td></td>").text(item.taskDateCreated))
                    .append($("<td></td>").text(item.taskDateUpdated));

                if (item.taskStatus !== 2) {
                    tr.append(
                        $("<td></td>").append(
                            $("<button>Edit</button>").on("click", function () {
                                editTask(item.taskId);
                            })
                        )
                    );
                }

                tr.append(
                    $("<td></td>").append(
                        $("<button>Delete</button>").on("click", function () {
                            deleteTask(item.taskId);
                        })
                    )
                );

                tr.appendTo(tBody);
            });

            tasks = data;
            renderPager($('#tasksPager'), taskCount, taskPage, taskPageSize, getTasks);
            $('#tasks-area').show();
        }
    });
}

function addTask() {
    const item = {
        taskName: $("#add-task-name").val(),
        taskDescription: $("#add-task-description").val(),
        taskPriority: $("#add-task-priority").val(),
        projectId: $("#select-project-id").val(),
    };

    $.ajax({
        type: "POST",
        accepts: "application/json",
        url: uriTask,
        contentType: "application/json",
        data: JSON.stringify(item),
        error: function (jqXHR, textStatus, errorThrown) {
            alert("Something went wrong!");
        },
        success: function (result) {
            getTasks();
            $("#add-task-name").val("");
            $("#add-task-description").val("");
            $("#add-task-priority").val("");
        }
    });
}

function deleteTask(taskId) {
    $.ajax({
        url: uriTask + "/" + taskId,
        type: "DELETE",
        error: function (jqXHR, textStatus, errorThrown) {
            alert("Something went wrong!");
        },
        success: function (result) {
            closeTaskInput();
            getTasks();
        }
    });
}

function editTask(taskId) {
    $.each(tasks, function (key, item) {
        if (item.taskId === taskId) {
            $("#edit-task-id").val(item.taskId);
            $("#edit-task-name").val(item.taskName);
            $("#edit-task-description").val(item.taskDescription);
            $("#edit-task-priority").val(item.taskPriority);
            $("#edit-task-status").val(item.taskStatus);
        }
    });
    $("#edit-task").show();
}

$(".edit-task-form").on("submit", function () {
    const item = {
        taskId: $("#edit-task-id").val(),
        taskName: $("#edit-task-name").val(),
        taskDescription: $("#edit-task-description").val(),
        taskPriority: $("#edit-task-priority").val(),
        taskStatus: $("#edit-task-status").val(),
    };

    $.ajax({
        url: uriTask + "/" + item.taskId,
        type: "PUT",
        accepts: "application/json",
        contentType: "application/json",
        data: JSON.stringify(item),
        error: function (jqXHR, textStatus, errorThrown) {
            alert("Something went wrong!");
        },
        success: function (result) {
            getTasks();
        }
    });

    closeTaskInput();
    return false;
});

function closeTaskInput() {
    $("#edit-task").hide();
}

$('.sortable-column').on("click", function () {
    let field = this.getAttribute('field');
    let sortDirection = parseInt($('#task-sort-direction').val());

    if (sortDirection) {
        $('#task-sort-direction').val(0);
    } else {
        $('#task-sort-direction').val(1);
    }

    $('#task-sort-field').val(field);

    getTasks();
});

$('#task-filter').on("click", function () {
    taskPage = 0;
    $('#task-filter-date-created-from').val($('#task-filter-date-created-from-input').val());
    $('#task-filter-date-created-to').val($('#task-filter-date-created-to-input').val());
    $('#task-filter-priority').val($('#task-filter-priority-input').val());

    getTasks();
});

function clearFilter() {
    $('#task-filter-date-created-from').val('');
    $('#task-filter-date-created-from-input').val('');
    $('#task-filter-date-created-to').val('');
    $('#task-filter-date-created-to-input').val('');
    $('#task-filter-priority').val('');
    $('#task-filter-priority-input').val('');
}

$('#task-clear-filter').on("click", function () {
    clearFilter();
    getTasks();
});