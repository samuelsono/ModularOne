import React, { useState } from 'react';
import { IonContent, IonPage } from '@ionic/react';
import { useHistory } from 'react-router-dom';
import { Button, Input, Text, Field, tokens } from '@fluentui/react-components';
import { api, setAuthToken } from '../services/api';

const muted = { color: tokens.colorNeutralForeground3 };

const Login: React.FC = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const history = useHistory();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      // 1. Fire credentials to your .NET endpoints
      const response = await api.post('/auth/login', {
        username: username,
        password: password
      });

      // 2. Extract token from your typical .NET response object (e.g., response.data.token)
      const token = response.data.token; 
      
      // 3. Save session token securely locally 
      localStorage.setItem('token', token);
      setAuthToken(token);

      // 4. Enter the logged-in tab shell
      history.replace('/tabs/dashboard');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Invalid username or password.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <IonPage>
      <IonContent className="ion-padding" style={{ '--background': '#f5f5f5', display: 'grid', placeItems: 'center' }}>
        <div style={{ maxWidth: '360px', margin: '100px auto 0 auto', padding: '20px' }}>
          
          <div style={{ textAlign: 'center', marginBottom: '32px' }}>
            <Text as="h1" weight="semibold" size={600}>Sign In</Text>
            <br />
            <Text style={muted}>Leave Management System</Text>
          </div>

          <form onSubmit={handleLogin} style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
            
            <Field label="Username">
              <Input 
                value={username} 
                onChange={(e, data) => setUsername(data.value)} 
                required 
                type="text"
              />
            </Field>

            <Field label="Password">
              <Input 
                value={password} 
                onChange={(e, data) => setPassword(data.value)} 
                required 
                type="password"
              />
            </Field>

            {error && (
              <Text style={{ color: '#d13438' }} size={200} block>
                {error}
              </Text>
            )}

            <Button appearance="primary" type="submit" disabled={loading} style={{ marginTop: '10px' }}>
              {loading ? 'Signing in...' : 'Sign In'}
            </Button>

          </form>
        </div>
      </IonContent>
    </IonPage>
  );
};

export default Login;
