$paths = @('D:\Agent', 'D:\Agent\Work', 'D:\Agent\Work\_Temp')

$paths | % {
    $d = [System.IO.Directory]::CreateDirectory($_)
    $acl = Get-Acl $d.FullName
    $ar = new-object System.Security.AccessControl.FileSystemAccessRule("Everyone", "FullControl", "ContainerInherit, ObjectInherit", "None", "Allow")
    $acl.AddAccessRule($ar)
    Set-Acl $d.FullName -AclObject $acl
}