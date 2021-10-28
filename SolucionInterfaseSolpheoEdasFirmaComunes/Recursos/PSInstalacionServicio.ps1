$params = @{
  Name = "ServicioSolucionFacturasEdasFirma"
  DisplayName = "Servicio Solucion Facturas EdasFirma"
  BinaryPathName = '"C:\Users\pberlinches\source\repos\Zerocoma_InterfaseEdasFirma\SolucionInterfaseSolpheoEdasFirmaComunes\bin\Debug\SolucionFacturasEdasFirmaServicio.exe solpheo"'
  StartupType = "Auto"
  Description = "Servicio Solucion Facturas EdasFirma"
}
New-Service @params