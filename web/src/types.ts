export type FlightResponse = {
    id: number
    flightNumber: string
    origin: string
    destination: string
    departureTime: string // JSON'da tarih tipi yok: "2026-12-01T08:00:00Z" gibi metin gelir
    arrivalTime: string
    totalSeats: number
    availableSeats: number
    price: number // C#'taki decimal; JavaScript'te tek sayı tipi var
}