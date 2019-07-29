$scopeMap = @{
	"Files.Read.Selected" = "5447fe39-cb82-4c1a-b977-520e67e724eb"
	"Files.ReadWrite.Selected" = "17dde5bd-8c17-420f-a486-969730c1b827"
	"Files.ReadWrite.AppFolder" = "8019c312-3263-48e6-825e-2b833497195b"
	"Reports.Read.All" = "02e97553-ed7b-43d0-ab3c-f8bace0d040c"
	"Sites.ReadWrite.All" = "89fe6a52-be36-487e-b7d8-d061c450a026"
	"Member.Read.Hidden" = "658aa5d8-239f-45c4-aa12-864f4fc7e490"
	"Tasks.ReadWrite.Shared" = "c5ddf11b-c114-4886-8558-8a4e557cd52b"
	"Tasks.Read.Shared" = "88d21fd4-8e5a-4c32-b5e2-4a1c95f34f72"
	"Contacts.ReadWrite.Shared" = "afb6c84b-06be-49af-80bb-8f3f77004eab"
	"Contacts.Read.Shared" = "242b9d9e-ed24-4d09-9a52-f43769beb9d4"
	"Calendars.ReadWrite.Shared" = "12466101-c9b8-439a-8589-dd09ee67e8e9"
	"Calendars.Read.Shared" = "2b9c4092-424d-4249-948d-b43879977640"
	"Mail.Send.Shared" = "a367ab51-6b49-43bf-a716-a1fb06d2a174"
	"Mail.ReadWrite.Shared" = "5df07973-7d5d-46ed-9847-1271055cbd51"
	"Mail.Read.Shared" = "7b9103a5-4610-446b-9670-80643382c1fa"
	"User.Read" = "e1fe6dd8-ba31-4d61-89e7-88639da4683d"
	"User.ReadWrite" = "b4e74841-8e56-480b-be8b-910348b18b4c"
	"User.ReadBasic.All" = "b340eb25-3456-403f-be2f-af7a0d370277"
	"User.Read.All" = "a154be20-db9c-4678-8ab7-66f6cc099a59"
	"User.ReadWrite.All" = "204e0828-b5ca-4ad8-b9f3-f32a958e7cc4"
	"Group.Read.All" = "5f8c59db-677d-491f-a6b8-5f174b11ec1d"
	"Group.ReadWrite.All" = "4e46008b-f24c-477d-8fff-7bb4ec7aafe0"
	"Directory.Read.All" = "06da0dbc-49e2-44d2-8312-53f166ab848a"
	"Directory.ReadWrite.All" = "c5366453-9fb0-48a5-a156-24f0c49a4b84"
	"Directory.AccessAsUser.All" = "0e263e50-5827-48a4-b97c-d940288653c7"
	"Mail.Read" = "570282fd-fa5c-430d-a7fd-fc8dc98a9dca"
	"Mail.ReadWrite" = "024d486e-b451-40bb-833d-3e66d98c5c73"
	"Mail.Send" = "e383f46e-2787-4529-855e-0e479a3ffac0"
	"Calendars.Read" = "465a38f9-76ea-45b9-9f34-9e8b0d4b0b42"
	"Calendars.ReadWrite" = "1ec239c2-d7c9-4623-a91a-a9775856bb36"
	"Contacts.Read" = "ff74d97f-43af-4b68-9f2a-b77ee6968c5d"
	"Contacts.ReadWrite" = "d56682ec-c09e-4743-aaf4-1a3aac4caa21"
	"Files.Read" = "10465720-29dd-4523-a11a-6a75c743c9d9"
	"Files.ReadWrite" = "5c28f0bf-8a70-41f1-8ab2-9032436ddb65"
	"Files.Read.All" = "df85f4d6-205c-4ac5-a5ea-6bf408dba283"
	"Files.ReadWrite.All" = "863451e7-0667-486c-a5d6-d135439485f0"
	"Sites.Read.All" = "205e70e5-aba6-4c52-a976-6d2d46c48043"
	"openid" = "37f7f235-527c-4136-accd-4a02d197296e"
	"offline_access" = "7427e0e9-2fba-42fe-b0c0-848c9e6a8182"
	"People.Read" = "ba47897c-39ec-4d83-8086-ee8256fa737d"
	"Notes.Create" = "9d822255-d64d-4b7a-afdb-833b9a97ed02"
	"Notes.ReadWrite.CreatedByApp" = "ed68249d-017c-4df5-9113-e684c7f8760b"
	"Notes.Read" = "371361e4-b9e2-4a3f-8315-2a301a3b0a3d"
	"Notes.ReadWrite" = "615e26af-c38a-4150-ae3e-c3b0d4cb1d6a"
	"Notes.Read.All" = "dfabfca6-ee36-4db2-8208-7a28381419b3"
	"Notes.ReadWrite.All" = "64ac0503-b4fa-45d9-b544-71a463f05da0"
	"Tasks.Read" = "f45671fb-e0fe-4b4b-be20-3d3ce43f1bcb"
	"Tasks.ReadWrite" = "2219042f-cab5-40cc-b0d2-16b1540b4c5f"
	"email" = "64a6cdd6-aab1-4aaf-94b8-3cc8405e90d0"
	"profile" = "14dad69e-099b-42c9-810b-d002981feec1"
}

function GetScopeId ($scope)
{
	Return $scopeMap[$scope]
}

function CreateScopeManifest($scopes) {
	$graphScopes = @{
		resourceAppId = "00000003-0000-0000-c000-000000000000"
		resourceAccess = @()
	}

	foreach ($s in $scopes) {
		$graphScopes.resourceAccess += @{
			id = $(GetScopeId($s))
			type = "Scope"
		}
	}

	$manifest = @($graphScopes)
	return $($manifest | ConvertTo-Json -Compress)
}