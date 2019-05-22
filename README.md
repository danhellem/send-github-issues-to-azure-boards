# send-github-issues-to-azure-boards
Have lots of GitHub repos you track? Use boards in Azure DevOps to track your work? Wouldn't it be great if you could consilidate all the issues that people post into a single Azure DevOps Board?

In this repo we have created a simple webhook reciever that takes an issue from any GitHub repo and places it on a Board of your choice in Azure DevOps.

![Animated Gif](https://github.com/danhellem/send-github-issues-to-azure-boards/blob/master/images/create-issue-send-to-azure-boards.gif "Animated Gif")

## How it works
As GitHub Issues are created, commented on, edited, and closed web hook payloads are sent to an the web hook reciever API. That API takes the content from the payload and creates a work item on a board. You can then manage the workflow on your board to match how your team does work. As comments are added or if the issue is changed, those events will post updates to the existing work item. If the Issue is closed in GitHub, the work item will automatically be closed in Azure Boards. Links are created on the work item so that your team can get back to the direct Issue as needed.

## Configuration
This is a .Net Core REST API with a single POST method. To setup you need to deploy the application to a hosted service like Azure or AWS. 

When you deploy make sure you adjust the appsettings.json to fit your Azure Boards and GitHub settings.

```
  "AppSettings": {
    "GitHub_Secret": "<secret from GitHub webhook>",
    "GitHub_Token": "",
    "GitHub_AppName": "",
    "ADO_Pat": "<azure devops personal acess token>",
    "ADO_Org": "<azure devops org name: danhellem (dev.azure.com/danhellem)>",
    "ADO_Project": "<azure devops project name>",
    "ADO_DefaultWIT": "<the work item type you want to create in azure boards. Examples: Issue, User Story>",
    "ADO_CloseState": "<the state on the work item type to close out the work item>",
    "ADO_NewState": "<the state on the work item type to reopen a work item>"
  }
```

Create a webhook and point to the published webhook. 

![config 1](https://github.com/danhellem/send-github-issues-to-azure-boards/blob/master/images/configure-webhook-1.png "Config 1")

Make sure you use the same secret in the web hook as you have defined in the GitHub_Secret setting above. Sent the content type to "Application/JSON"

Set your web hook events for "Issues" and "Issue Comments"

![config 2](https://github.com/danhellem/send-github-issues-to-azure-boards/blob/master/images/configure-webhook-2.png "Config 2")

## What is missing
- Coded in the configuration of New and Completed. Would like to be able to dynamically set this by work item type.
- Not all updates are being tracked and added into the work item. Description for example or Assigned To.
- not all events are being tracked. Example: Delete

## Questions
Post your issues here or dm me on twitter [@danhellem](https://twitter.com/danhellem)
