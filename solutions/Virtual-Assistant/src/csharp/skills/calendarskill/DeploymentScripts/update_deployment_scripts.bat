ECHO Updating deployment files in Calendar Skill
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\en\general.lu -o %0\..\en -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\en\calendar.lu -o %0\..\en -n Calendar --out calendar.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\de\general.lu -o %0\..\de -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\de\calendar.lu -o %0\..\de -n Calendar --out calendar.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\es\general.lu -o %0\..\es -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\es\calendar.lu -o %0\..\es -n Calendar --out calendar.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\fr\general.lu -o %0\..\fr -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\fr\calendar.lu -o %0\..\fr -n Calendar --out calendar.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\it\general.lu -o %0\..\it -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\it\calendar.lu -o %0\..\it -n Calendar --out calendar.luis
call ludown parse toluis --in %0\..\..\..\..\assistant\CognitiveModels\LUIS\zh-hans\general.lu -o %0\..\zh-hans -n General --out general.luis
call ludown parse toluis --in %0\..\..\CognitiveModels\LUIS\zh-hans\calendar.lu -o %0\..\zh-hans -n Calendar --out calendar.luis