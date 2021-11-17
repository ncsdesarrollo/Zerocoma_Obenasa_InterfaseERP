

$params = @{
  Name = "ZerocomaServicioSolucionFacturasNOMBREJSONDELTENANT"
  DisplayName = "Zerocoma - Servicio Solucion Facturas - NOMBREJSONDELTENANT"
  BinaryPathName = '"C:\Proyectos\Zerocoma Solucion Facturas Preproduccion\Servicio Windows\SolucionFacturasServicio.exe" NOMBREJSONDELTENANT'
  StartupType = "Auto"
  Description = "Servicio Zerocoma de facturas"
}
New-Service @params