import { Redirect, Route } from 'react-router-dom';
import { IonApp, IonRouterOutlet, setupIonicReact } from '@ionic/react';
import { IonReactRouter } from '@ionic/react-router';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import React, { useEffect } from 'react';

import Login from './pages/Login';
import MainTabs from './pages/MainTabs';
import { setAuthToken } from './services/api';

/* Core Ionic CSS (Required for page routing and device shells) */
import '@ionic/react/css/core.css';
import '@ionic/react/css/structure.css';
import '@ionic/react/css/typography.css';
import './theme/variables.css';

setupIonicReact();

function hasToken(): boolean {
  return Boolean(localStorage.getItem('token'));
}

const App: React.FC = () => {
  useEffect(() => {
    const existingToken = localStorage.getItem('token');
    if (existingToken) {
      setAuthToken(existingToken);
    }
  }, []);

  return (
    <FluentProvider theme={webLightTheme}>
      <IonApp>
        <IonReactRouter>
          <IonRouterOutlet>
            <Route exact path="/login" component={Login} />
            <Route path="/tabs" component={MainTabs} />
            <Route exact path="/dashboard">
              <Redirect to="/tabs/dashboard" />
            </Route>
            <Route exact path="/">
              {hasToken() ? <Redirect to="/tabs/dashboard" /> : <Redirect to="/login" />}
            </Route>
          </IonRouterOutlet>
        </IonReactRouter>
      </IonApp>
    </FluentProvider>
  );
};

export default App;
