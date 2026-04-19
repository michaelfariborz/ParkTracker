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
            const color = park.visited ? '#e63946' : '#adb5bd';
            const icon = L.divIcon({
                className: '',
                html: `<div style="
                    width: 14px;
                    height: 14px;
                    border-radius: 50%;
                    background-color: ${color};
                    border: 2px solid white;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.4);
                "></div>`,
                iconSize: [14, 14],
                iconAnchor: [7, 7]
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
