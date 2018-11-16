ECHO Updating deployment files in ToDo Skill
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\en\general.lu -o %0\..\en -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\todo.lu -o %0\..\en -n ToDo --out todo.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\de\general.lu -o %0\..\de -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\de\todo.lu -o %0\..\de -n ToDo --out todo.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\es\general.lu -o %0\..\es -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\es\todo.lu -o %0\..\es -n ToDo --out todo.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\fr\general.lu -o %0\..\fr -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\fr\todo.lu -o %0\..\fr -n ToDo --out todo.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\it\general.lu -o %0\..\it -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\it\todo.lu -o %0\..\it -n ToDo --out todo.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\zh\general.lu -o %0\..\zh -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\zh\todo.lu -o %0\..\zh -n ToDo --out todo.luis