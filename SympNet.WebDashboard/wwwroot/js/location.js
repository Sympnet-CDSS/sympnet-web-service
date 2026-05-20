window.initMap = function (dotNetHelper) {
    if (!navigator.geolocation) {
        return Promise.reject("La géolocalisation n'est pas supportée par votre navigateur.");
    }

    const mapElement = document.getElementById("map");
    if(mapElement) mapElement.style.display = "block";

    return new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
            async (position) => {
                const lat = position.coords.latitude;
                const lng = position.coords.longitude;

                await initializeLeafletMap(lat, lng, dotNetHelper);
                
                try {
                    const address = await reverseGeocode(lat, lng);
                    resolve({ lat: lat, lng: lng, address: address });
                } catch(e) {
                    resolve({ lat: lat, lng: lng, address: "" });
                }
            },
            (error) => {
                let msg = "Erreur inconnue";
                switch(error.code) {
                    case error.PERMISSION_DENIED: msg = "Permission refusée."; break;
                    case error.POSITION_UNAVAILABLE: msg = "Position indisponible."; break;
                    case error.TIMEOUT: msg = "Délai expiré."; break;
                }
                reject(msg);
            },
            { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
        );
    });
};

let mapInstance = null;
let markerInstance = null;

async function initializeLeafletMap(lat, lng, dotNetHelper) {
    if (mapInstance) {
        mapInstance.remove();
    }

    mapInstance = L.map('map').setView([lat, lng], 16);
    
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(mapInstance);

    // Default Leaflet icon fix for some environments
    const icon = L.icon({
        iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
        iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
        shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41]
    });

    markerInstance = L.marker([lat, lng], { draggable: true, icon: icon }).addTo(mapInstance);

    // Fix map rendering issue when unhiding the div
    setTimeout(() => { mapInstance.invalidateSize(); }, 300);

    markerInstance.on('dragend', async function (event) {
        const marker = event.target;
        const position = marker.getLatLng();
        
        try {
            const address = await reverseGeocode(position.lat, position.lng);
            dotNetHelper.invokeMethodAsync('UpdateLocation', position.lat, position.lng, address);
        } catch(e) {
            console.error(e);
        }
    });
}

async function reverseGeocode(lat, lng) {
    const url = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`;
    const response = await fetch(url, { headers: { 'Accept-Language': 'fr' }});
    if (!response.ok) throw new Error("Erreur geocoding");
    const data = await response.json();
    
    // Use the full display name provided by Nominatim as it contains all exact details (building, POI, etc)
    return data.display_name || "Adresse inconnue";
}
