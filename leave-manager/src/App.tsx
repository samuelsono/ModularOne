import { Redirect, Route, useHistory } from 'react-router-dom';
import { IonApp, IonRouterOutlet, setupIonicReact } from '@ionic/react';
import { IonReactRouter } from '@ionic/react-router';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import React, { useEffect } from 'react';

import Login from './pages/Login';
import MainTabs from './pages/MainTabs';
import { restoreSession, SESSION_EXPIRED_EVENT } from './services/authService';
import { hasStoredAccessToken } from './services/tokenStorage';

/* Core Ionic CSS (Required for page routing and device shells) */
import '@ionic/react/css/core.css';
import '@ionic/react/css/structure.css';
import '@ionic/react/css/typography.css';
import './theme/variables.css';

setupIonicReact();

const SessionExpiredListener: React.FC = () => {
  const history = useHistory();

  useEffect(() => {
    const onExpired = () => {
      history.replace('/login');
    };
    window.addEventListener(SESSION_EXPIRED_EVENT, onExpired);
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired);
  }, [history]);

  return null;
};

const App: React.FC = () => {
  useEffect(() => {
    restoreSession();
  }, []);

  return (
    <FluentProvider theme={webLightTheme}>
      <IonApp>
        <IonReactRouter>
          <SessionExpiredListener />
          <IonRouterOutlet>
            <Route exact path="/login" component={Login} />
            <Route path="/tabs" component={MainTabs} />
            <Route exact path="/dashboard">
              <Redirect to="/tabs/dashboard" />
            </Route>
            <Route exact path="/">
              {hasStoredAccessToken() ? <Redirect to="/tabs/dashboard" /> : <Redirect to="/login" />}
            </Route>
          </IonRouterOutlet>
        </IonReactRouter>
      </IonApp>
    </FluentProvider>
  );
};

export default App;
