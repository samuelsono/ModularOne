import axios from 'axios';

// Replace with your actual deployed .NET API URL
// NOTE: If testing on an Android Emulator, use 'http://10.0.2.2:5001' instead of localhost
const API_BASE_URL = 'http://localhost:51705/api'; 

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Helper to append JWT tokens to subsequent requests after logging in
export const setAuthToken = (token: string | null) => {
  if (token) {
    api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  } else {
    delete api.defaults.headers.common['Authorization'];
  }
};
