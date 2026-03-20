# Update functionality
The Umbraco.Community.LegacyFeatureConverter is a fork of the AuthBlockList package (https://github.com/Lantzify/auto-block-list). This Umbraco package is meant for converting nested content and macro's into Block lists and blocks.
In my package I want to take the solid foundation that is there and reshape it into different functionality. The conversion logic for the nested content and macro's is good but I want to have this package behave differently.

There are three things I want to be able to convert with this package:
- Nested content (conversion logic is present)
- Macro's (conversion logic is present)
- Older legacy media picker property editors to the MediaPicker3 media picker (this functionality does not exist yet)

Consider these seperate functionalities as three different features of the package. This way we can easily manage the development and testing of each feature separately. So you update them independently and you can also choose to use only one of the features if you want to. For example, if you only want to convert nested content, you can just use that feature without having to worry about the macro conversion or the media picker conversion.

## Nested content conversion
The current functionality works like this:
- Go to the backoffice
- Select pages you want to convert by selecting them
- Run the migration. This converts the nested content of the selected pages into block lists. It does this by:
	- First creating the required data types
	- Then it adds a new property to the document type with the same alias, suffixed with 'BL'.
	- The converted nested content data is then placed in the newly created block list property.

This strategy has some downsides, or I just want to see differently:
- The conversion is based on the content. So if there is a nested content property on a document type, but there is no content using it, it will not be converted. This means that the document type will not get the new block list property and the conversion will not be complete.
- It creates a new property for each nested content property, which can lead to a lot of properties if there are many nested content properties. This can make the document type cluttered and hard to manage. You also need to clean up the old nested content properties manually after the conversion, which is an extra step.
- The conversion is a one-time thing. If you create new content using the old nested content property after the conversion, it will not be converted to the block list format. This can lead to inconsistencies in your content and requires you to run the conversion again.

So I want to have the following strategy for the nested content conversion:
- First check all document types for nested content properties. If there are any, create the required data types and add the new block list property to the document type. This way we ensure that all document types are ready for the conversion, even if there is no content using the nested content properties yet.
- Then one by one we update the document types properties for nested content to block lists.
- Then for all implementations (content nodes) of the document type, we convert the existing nested content data to the block list format and place it in the new block list property.

When everything is done, we can remove the old nested content properties from the document types. This way we have a clean conversion and we don't have to worry about inconsistencies in the content. We now have the data migrated to the new block list format and we can safely remove the old nested content properties.

## Macro conversion
I currently do not yet know how the macro conversion works, so we'll skip it for now.

## Media picker conversion
I want the media picker conversion to work like the nested content conversion. So first we check all document types for media picker properties that are using the older legacy media picker. If there are any, we create the required data types and add the new media picker property to the document type. This way we ensure that all document types are ready for the conversion, even if there is no content using the old media picker properties yet.
Then one by one we update the document types properties for the old media picker to the new media picker. Then for all implementations (content nodes) of the document type, we convert the existing media picker data to the new media picker format and place it in the new media picker property.
When everything is done, we can remove the old media picker properties from the document types. This way we have a clean conversion and we don't have to worry about inconsistencies in the content. We now have the data migrated to the new media picker format and we can safely remove the old media picker properties.

## Test run 
For any of the conversion, I want the user to be able to do a test run. This will do everything an actual conversion would do, but it will not actually save any changes to the database. This acts like a test to see if the conversion will very likely succeed. It will catch things like invalid values in properties for instance.

## Logging and history
I want every conversion to be logged in a history overview. Each run should create a log entry and they entry should be openable to see the details of the conversion. This also includes test runs. The idea is that
you can easily see if everything went well and if not, to see what and where it went wrong. It's important that we provide as much helpful information is possible if the conversion goes wrong.

## Miscellaneous
- I want to have the option to select which document types to convert. This way you can choose to only convert certain document types if you want to. For example, if you only want to convert the news articles, you can just select the news article document type and run the conversion for that.
- We need to decide when we abort the conversion, and when to just go to the next item. For example, if we encounter an error while converting a content node, do we want to abort the entire conversion or do we want to just log the error and continue with the next content node? I think it would be best to just log the error and continue with the next item, so that we can convert as much as possible and not have to worry about one error stopping the entire conversion.
- We need to decide if we want to have a rollback option. This would allow us to undo the conversion if something goes wrong. This can be useful if we encounter a lot of errors during the conversion and we want to revert back to the original state. However, this can also be a lot of work to implement and it can also be risky if not done correctly. So we need to weigh the pros and cons of having a rollback option and decide if it's worth implementing or not. Maybe it's possible to manually create a record in the audit trail/history of the content node to do this.
- Because both the media picker conversion and nested content conversion are essentially the same, just with different property editors, we can reuse a lot of the code for both conversions. We can create a base conversion class that contains the common logic for both conversions and then create separate classes for the nested content conversion and the media picker conversion that inherit from the base class and implement the specific logic for each conversion. This way we can avoid code duplication and make the code more maintainable. This also opens the door for more converters in the future.

## Development
- Don't guess or make assumptions. Always research everything and provide proof. You have access to the Umbraco source code for verification.
- I want clean and maintanable code. This is going to be an open source package, so it needs to be easy to understand and of high quality and maintainable. This also means that we need to have good documentation and comments in the code, so that other developers can easily understand how the code works and how to use it.
- This package is only for Umbraco 13, NOT for older and NOT for newer versions. So when researching, make sure you pick the correct Umbraco version.
- I want to have good test coverage for the code. This is important to ensure that the code works as expected and to catch any bugs or issues before they go live. We use testing framework MSTest. The goal is the test the important logic in the code. Getting 100% code coverage is not a goal by itself.
- If you are unsure about something, or if you see improvements, always ask. Also never decide by yourself that something is too complex and skip it. Always ask if it's something that we want to have in the package or not, and if it's worth the effort to implement it or not. It's better to ask and have a discussion about it than to just skip it and potentially miss out on important functionality or improvements.

