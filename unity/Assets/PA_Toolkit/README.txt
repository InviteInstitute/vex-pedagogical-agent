----------=Downloading an Agent from the Web App=----------
1. Click 'Create new agent' 
2. Customize your agent
3. Click 'Finish' to name and save your agent. 
4. Click the 'Share' icon 
5. Click 'Download Toolkit Package'
	This will download PA_Toolkit.unitypackage
6. Click 'Export Agent File'
	This will download a .agent file

Send your developer the PA_Toolkit.unitypackage file along with any number of .agent files

Note: As the web application changes, the PA_Toolkit may become incompatable with older versions. 
Always send the newest PA_Toolkit.unitypackage file

Note: Currently, if the web app is refreshed, you will lose your progress

----------=Setting up your first Agent using PA_Toolkit=----------
1. Create a new Unity Project
    (Use Unity Version 2022.3.17f1 for the most stable results)
2. Drag 'PA_Toolkit.unitypackage' into your Assets folder
3. Click 'Import' on the popup
4. Add an Agent to your scene
	Either
	a) Right click in your Hierarcy and navigate to '3D Object -> Agent'
	or
	b) Navigate to 'PA_Toolkit -> prefabs -> agent' and drag the prefab into your scene
5. You will be prompted to install dependent packages, packages may take up to a minute to download, please wait
6. After all packages are installed you will be prompted to restart the Editor

You are now ready to load your agent(s) to your new Unity Project

----------=Loading an Agent using PA_Toolkit=----------
1. Add an Agent to your scene
	Either
	a) Right click in your Hierarcy and navigate to '3D Object -> Agent'
	or
	b) Navigate to 'PA_Toolkit -> prefabs -> agent' and drag the prefab into your scene
2. Load your .agent file by selecting your Agent in the hierarcy
	Either 
	a) Click 'Load Agent from file' in the 'Agent' component
	or
	b) Click 'PA_Toolkit -> Load agent(s) from file (.agent)' in the top toolbar
3. Navigate and select a .agent file

Your agent is now loaded

----------=Using Predefined Gestures and Expressions using PA_Toolkit=----------
1. Create a serialized reference to an 'ActionSetting' in your script. (actionSettingToPlay)
2. Get a reference to your desired agent. (targetAgent)
3. call 'targetAgent.PlayAction(actionSettingToPlay);' To play an action

Actions can be stopped by calling 'agent.StopAction();'

ActionSettings have the following cusomizeable parameters
Name: Custom name for your action, by default is not read
Text: A transcription of your audio, by default is not read. 
Audio: The audio file to play when this action is called (PA_Toolkit includes sample audio for testing purposes)
Expression: The facial expression to play when this action is called
Gesture: The gesture animation to play when this action is called