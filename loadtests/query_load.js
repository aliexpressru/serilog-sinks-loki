import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    vus: 1000,
    stages: [
        { duration: '5m', target: 10000 }
    ],
};

const params = {
    headers: {
        'Content-Type': 'application/json',
    },
};

const url = 'http://localhost:5000/create';

export default function () {
    
    http.post(url, JSON.stringify(
        {
            "id": 1,
            "name": "8810cc78-6273-4882-bcf0-e092a1d08494",
            "username": "a4de2a85-867d-4f57-b205-fc567981888c",
            "email": "40d57a99-921f-41b5-a887-6853c413f1f6",
            "address": {
                "street": "1175bac6-ba1e-49a9-adc6-2688ded105d2",
                "suite": "7d63596d-d5b7-423d-91c9-8d72f3cd9eb9",
                "city": "25c0e2c9-f362-4ec9-92aa-9b877ed40bfa",
                "zipcode": "a9520d93-60d2-467c-a161-626641a9baaf",
                "geo": {
                    "lat": "f8cac50b-e5d7-4c79-b0a8-d759834a6217",
                    "lng": "f69790f0-b222-4ddc-ba65-2b57a59af798"
                }
            },
            "phone": "bfb9b151-0ef4-4edd-b2a1-21dc69811ab8",
            "website": "3c272ac1-04af-410b-8ecb-9fd88bea05dd",
            "company": {
                "name": "7c22e166-c96c-4ce7-b50b-bfc6cb0c2e22",
                "catchPhrase": "42b4d76b-5fd4-46b1-8d65-cd42d1e02cae",
                "bs": "41107c2a-75a7-4dd9-8930-c2ea5eabfcc3"
            }
        }), params);
    
    sleep(1);
}