ECHO Updating deployment files in News Skill

call ludown parse toluis --in ..\..\..\..\..\assistant\CognitiveModels\LUIS\en\general.lu -o %0\..\en -n General --out general.luis
call ludown parse toluis --in ..\CognitiveModels\LUIS\en\news.lu -o %0\..\en -n news --out news.luis

