window.leafletInterop = (function () {
    let map = null;
    let markers = [];
    let dotNetRef = null;

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function initMap(containerId, lat, lon, zoom) {
        if (map) {
            map.remove();
            map = null;
            markers = [];
        }
        map = L.map(containerId).setView([lat, lon], zoom);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(map);
    }

    function addPins(parks, dotNetObjRef) {
        dotNetRef = dotNetObjRef;
        parks.forEach(function (park) {
            const fillColor = park.visited ? '#a8e063' : '#9ca3af';
            const strokeColor = park.visited ? '#1b4332' : '#6b7280';
            const icon = L.divIcon({
                className: '',
                html: `<svg width="20" height="30" viewBox="0 0 24 36" xmlns="http://www.w3.org/2000/svg" style="filter:drop-shadow(0 2px 3px rgba(0,0,0,0.3))"><path d="M12 0C5.373 0 0 5.373 0 12c0 9 12 24 12 24s12-15 12-24C24 5.373 18.627 0 12 0z" fill="${fillColor}" stroke="${strokeColor}" stroke-width="1.5"/><circle cx="12" cy="12" r="4" fill="white" opacity="0.85"/></svg>`,
                iconSize: [20, 30],
                iconAnchor: [10, 30],
                popupAnchor: [0, -32]
            });

            let popupHtml = `<strong>${escapeHtml(park.name)}</strong><br>${escapeHtml(park.state)}`;
            if (park.visited && park.visitDates && park.visitDates.length > 0) {
                const dates = park.visitDates
                    .map(d => d ? new Date(d).toLocaleDateString() : 'Date not recorded')
                    .join('<br>');
                popupHtml += `<br><em>Visited:</em><br>${dates}`;
            }
            popupHtml += `<br><button class="btn btn-sm btn-primary mt-2" onclick="leafletInterop.openAddVisit(${park.id})">+ Add Visit</button>`;

            const marker = L.marker([park.latitude, park.longitude], { icon })
                .addTo(map)
                .bindPopup(popupHtml);

            markers.push(marker);
        });
    }

    function clearPins() {
        markers.forEach(function (marker) {
            marker.remove();
        });
        markers = [];
    }

    function openAddVisit(parkId) {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OpenAddVisitFromMap', parkId);
        }
    }

    return { initMap, addPins, clearPins, openAddVisit };
})();
