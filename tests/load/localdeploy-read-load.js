import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = (__ENV.BASE_URL || 'http://host.docker.internal').replace(/\/$/, '');

export const options = {
  stages: [
    { duration: '10s', target: 3 },
    { duration: '30s', target: 5 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

const endpoints = [
  '/health',
  '/api/tasks',
  '/api/tasks/summary',
  '/api/activity',
];

export default function () {
  for (const endpoint of endpoints) {
    const response = http.get(`${baseUrl}${endpoint}`);

    check(response, {
      [`${endpoint} returned 200`]: (res) => res.status === 200,
    });
  }

  sleep(1);
}
