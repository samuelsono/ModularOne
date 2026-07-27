import React, { useMemo } from 'react';
import { IonContent, IonPage } from '@ionic/react';
import { Button, Card, Text, Persona, tokens } from '@fluentui/react-components';
import { SignOut24Regular } from '@fluentui/react-icons';
import { useHistory } from 'react-router-dom';
import { clearSession, displayNameForUser, getStoredUser } from '../services/authService';

const muted = { color: tokens.colorNeutralForeground3 };

const Profile: React.FC = () => {
  const history = useHistory();
  const user = useMemo(() => getStoredUser(), []);

  const handleSignOut = () => {
    clearSession();
    history.replace('/login');
  };

  const name = displayNameForUser(user);
  const secondary = user?.email || user?.username || 'Remote access';
  const roles = user?.roles?.length ? user.roles.join(', ') : null;

  return (
    <IonPage>
      <IonContent className="ion-padding app-page">
        <div className="app-page-header">
          <Text as="h1" weight="semibold" size={600}>
            Profile
          </Text>
          <Text style={muted}>Your account for remote leave and claims</Text>
        </div>

        <Card className="app-card">
          <Persona
            name={name}
            secondaryText={secondary}
            avatar={{ color: 'colorful', idForColor: user?.email || user?.username || 'user' }}
            size="extra-large"
            textAlignment="center"
          />
          <div style={{ marginTop: 16, textAlign: 'center' }}>
            {roles && (
              <Text size={200} block style={{ ...muted, marginBottom: 4 }}>
                {roles}
              </Text>
            )}
            <Text size={200} block style={muted}>
              Signed in to Chronos Leave & Claims
            </Text>
          </div>
        </Card>

        <Card className="app-card">
          <Text weight="semibold" block>
            Account
          </Text>
          <Text size={200} block style={{ ...muted, marginTop: 8 }}>
            Username: {user?.username || '—'}
          </Text>
          <Text size={200} block style={{ ...muted, marginTop: 4 }}>
            Email: {user?.email || '—'}
          </Text>
        </Card>

        <Button
          appearance="secondary"
          icon={<SignOut24Regular />}
          onClick={handleSignOut}
          style={{ width: '100%', marginTop: 8 }}
        >
          Sign out
        </Button>
      </IonContent>
    </IonPage>
  );
};

export default Profile;
