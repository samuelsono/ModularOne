import { Outlet } from "react-router-dom";
import { useStyles } from '../../../main';
import { Card, Divider, Link } from "@fluentui/react-components";

function AuthLayout() {

  const styles = useStyles();
  return (
    <div className="flex flex-col">
       <div className="flex flex-row max-w-[100vw] h-[100vh] overflow-hidden">
             <div className={`${styles.content} flex items-center flex-col justify-center h-full w-full overflow-hidden`}>
                <div className={"absolute top-0 left-0 w-full h-[50vh] bg-white"}></div>

                <img src="/logo.png" alt="Aspire Logo" className="w-20 z-20 h-auto mb-3" />
                <Card className="w-full max-w-sm p-5!">
                 <Outlet />
                </Card>
                <ul className="flex gap-3 py-3 text-xs">
                    <li><Link href="/terms">Terms of use</Link></li>
                    <li><Divider vertical /></li>
                    <li><Link href="/privacy">Privacy policy</Link></li>
                </ul>
            </div>
      </div>
    </div>
  );
}

export default AuthLayout;
