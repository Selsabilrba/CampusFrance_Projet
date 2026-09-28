using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CampusFrance_Projet.StepDefinitions
{
    [Binding]
    public class CampusFranceAccountRegistrationStepDefinitions
    {
        private IWebDriver? driver;
        private WebDriverWait? wait;
        private readonly ScenarioContext scenarioContext;

        // Temps (secondes) pendant lequel Chrome reste ouvert après la fin du scénario
        private const int PauseSeconds = 10;

        public CampusFranceAccountRegistrationStepDefinitions(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        private IWebDriver Drv =>
            driver ?? throw new Exception("Le navigateur n'est pas démarré.");

        private WebDriverWait Waiter =>
            wait ?? throw new Exception("WebDriverWait non disponible.");


        // AVANT / APRES CHAQUE SCENARIO


        [BeforeScenario]
        public void BeforeScenario()
        {
            Console.WriteLine("=== AVANT LE SCENARIO ===");

            var options = new ChromeOptions();

            // Jenkins définit automatiquement JENKINS_URL
            bool isCi = Environment.GetEnvironmentVariable("JENKINS_URL") != null;

            if (isCi)
            {
                options.AddArgument("--headless=new");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
            }
            else
            {
                options.AddArgument("--start-maximized");
            }

            string? driverDir = Environment.GetEnvironmentVariable("CHROMEDRIVER_DIR");

            ChromeDriverService service = string.IsNullOrWhiteSpace(driverDir)
                ? ChromeDriverService.CreateDefaultService()
                : ChromeDriverService.CreateDefaultService(driverDir);

            service.InitializationTimeout = TimeSpan.FromSeconds(90);
            service.HideCommandPromptWindow = true;

            driver = new ChromeDriver(service, options);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            driver.Navigate().GoToUrl("https://www.campusfrance.org/fr/user/register");

            // Attendre que le formulaire d'inscription soit affiché
            Waiter.Until(d =>
                d.FindElements(By.Id("user-form")).Any(e => e.Displayed));

            DismissCookieBanner();

            Console.WriteLine("=== PAGE CAMPUS FRANCE OUVERTE ===");
        }

        [AfterScenario]
        public void AfterScenario()
        {
            Console.WriteLine("=== FIN DU SCENARIO ===");

            try
            {
                if (scenarioContext.TestError != null)
                {
                    Console.WriteLine("=== SCENARIO EN ECHEC ===");
                    Console.WriteLine(scenarioContext.TestError.Message);

                    if (driver is ITakesScreenshot shot)
                    {
                        string path = Path.Combine(
                            AppContext.BaseDirectory,
                            "echec_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");

                        shot.GetScreenshot().SaveAsFile(path);
                        Console.WriteLine("Capture d'écran : " + path);
                    }
                }

                // Laisse le temps de voir l'état final de la page
                if (Environment.GetEnvironmentVariable("JENKINS_URL") == null)
                    Thread.Sleep(PauseSeconds * 1000);

                driver?.Quit();
                driver?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur à la fermeture : " + ex.Message);
            }
            finally
            {
                driver = null;
                wait = null;
            }
        }

        
        // GIVEN
       

        [Given("user is on the Campus France account creation page")]
        public void GivenUserIsOnTheCampusFranceAccountCreationPage()
        {
            Console.WriteLine("URL actuelle : " + Drv.Url);

            if (!Drv.Url.Contains("campusfrance.org"))
                throw new Exception("La page Campus France n'est pas ouverte.");

            if (!Drv.FindElements(By.Id("user-form")).Any(e => e.Displayed))
                throw new Exception("Le formulaire d'inscription n'est pas affiché.");
        }

       
        // EMAIL + MOT DE PASSE
        // (dans ce formulaire, l'email est le champ "name")
       

        [When("user fills in the common fields with user fills in fields {string}, {string}, {string}")]
        public void WhenUserFillsInTheCommonFieldsWithUserFillsInFields(
            string email,
            string motDePasse,
            string confirmationMotDePasse)
        {
            Console.WriteLine("=== EMAIL / MOT DE PASSE ===");

            // Attention : #edit-name existe aussi dans le formulaire de
            // connexion du header, on cible donc #user-form.
            FillBy(By.CssSelector("#user-form #edit-name"), email);
            FillBy(By.Id("edit-pass-pass1"), motDePasse);
            FillBy(By.Id("edit-pass-pass2"), confirmationMotDePasse);
        }

       
        // INFORMATIONS PERSONNELLES
    

        [When("user fills in {string}, {string}, {string}, {string}, {string}, {string}, {string} and {string}")]
        public void WhenUserFillsIn(
            string civilite,
            string nom,
            string prenom,
            string paysResidence,
            string nationalite,
            string codePostal,
            string ville,
            string telephone)
        {
            Console.WriteLine("=== INFORMATIONS PERSONNELLES ===");

            // Civilité : boutons radio (ids edit-field-civilite-mr / -mme)
            if (!string.IsNullOrWhiteSpace(civilite))
            {
                string key = Normalize(civilite).TrimEnd('.');
                ClickRadio("edit-field-civilite-" + key);
            }

            if (!string.IsNullOrWhiteSpace(nom))
                FillBy(By.Id("edit-field-nom-0-value"), nom);

            if (!string.IsNullOrWhiteSpace(prenom))
                FillBy(By.Id("edit-field-prenom-0-value"), prenom);

            // Pays de résidence : liste Selectize
            if (!string.IsNullOrWhiteSpace(paysResidence))
                SelectizeSelect("edit-field-pays-concernes", paysResidence);

            // Nationalité : autocomplete Drupal
            if (!string.IsNullOrWhiteSpace(nationalite))
                FillAutocomplete("edit-field-nationalite-0-target-id", nationalite);

            if (!string.IsNullOrWhiteSpace(codePostal))
                FillBy(By.Id("edit-field-code-postal-0-value"), codePostal);

            if (!string.IsNullOrWhiteSpace(ville))
                FillBy(By.Id("edit-field-ville-0-value"), ville);

            if (!string.IsNullOrWhiteSpace(telephone))
                FillBy(By.Id("edit-field-telephone-0-value"), telephone);
        }

        // TYPE DE PROFIL (radios : 2 = Étudiants, 3 = Chercheurs, 4 = Institutionnel)
        

        [When("user selects the {string}")]
        public void WhenUserSelectsThe(string typeProfil)
        {
            Console.WriteLine("=== TYPE DE PROFIL : " + typeProfil + " ===");

            if (string.IsNullOrWhiteSpace(typeProfil))
                return;

            string p = Normalize(typeProfil);
            string value;

            if (p.StartsWith("etud")) value = "2";
            else if (p.StartsWith("cherch")) value = "3";
            else if (p.StartsWith("instit")) value = "4";
            else throw new Exception("Type de profil inconnu : " + typeProfil);

            ClickRadio("edit-field-publics-cibles-" + value);

            // Les champs suivants n'apparaissent qu'après ce choix
            // (Drupal #states) : on laisse le JS de la page s'exécuter.
            Thread.Sleep(800);
        }

        // INFORMATIONS PROFESSIONNELLES
     

        [When("user fills in field {string}, {string}, {string}, {string}, {string}")]
        public void WhenUserFillsInField(
            string domaine,
            string niveauEtudes,
            string fonction,
            string organismeType,
            string organismeNom)
        {
            Console.WriteLine("=== INFORMATIONS PROFESSIONNELLES ===");

            if (!string.IsNullOrWhiteSpace(domaine))
                SelectizeSelect("edit-field-domaine-etudes", domaine);

            if (!string.IsNullOrWhiteSpace(niveauEtudes))
                SelectizeSelect("edit-field-niveaux-etude", niveauEtudes);

            if (!string.IsNullOrWhiteSpace(fonction))
                FillBy(By.Id("edit-field-fonction-0-value"), fonction);

            if (!string.IsNullOrWhiteSpace(organismeType))
                SelectizeSelect("edit-field-type-organisme", organismeType);

            if (!string.IsNullOrWhiteSpace(organismeNom))
                FillBy(By.Id("edit-field-nom-organisme-0-value"), organismeNom);
        }

        // VERIFICATION DU BOUTON
       

        [Then("The bouton name is {string}")]
        public void ThenTheBoutonNameIs(string button)
        {
            var submits = Drv.FindElements(
                By.CssSelector("#user-form input[type='submit'], #user-form button"));

            bool found = submits.Any(b =>
                b.Displayed &&
                (
                    b.Text.Trim().Equals(button, StringComparison.OrdinalIgnoreCase) ||
                    (b.GetAttribute("value") ?? "").Trim()
                        .Equals(button, StringComparison.OrdinalIgnoreCase)
                ));

            if (!found)
                throw new Exception($"Le bouton '{button}' n'a pas été trouvé.");

            Console.WriteLine("Bouton trouvé : " + button);
        }

       

        /// <summary>Ferme la bannière cookies (tarteaucitron) qui peut recouvrir le formulaire.</summary>
        private void DismissCookieBanner()
        {
            try
            {
                var shortWait = new WebDriverWait(Drv, TimeSpan.FromSeconds(5));

                var deny = shortWait.Until(d =>
                    d.FindElements(By.Id("tarteaucitronAllDenied2"))
                     .FirstOrDefault(e => e.Displayed));

                JsClick(deny);
                Thread.Sleep(500);
                Console.WriteLine("Bannière cookies fermée.");
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Pas de bannière cookies.");
            }
        }

        private IWebElement FindVisible(By by)
        {
            return Waiter.Until(d =>
                d.FindElements(by).FirstOrDefault(e => e.Displayed && e.Enabled))
                ?? throw new Exception("Élément introuvable : " + by);
        }

        private void ScrollTo(IWebElement element)
        {
            ((IJavaScriptExecutor)Drv).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});", element);
            Thread.Sleep(200);
        }

        private void JsClick(IWebElement element)
        {
            ((IJavaScriptExecutor)Drv).ExecuteScript("arguments[0].click();", element);
        }

        private void TryClick(IWebElement element)
        {
            try
            {
                element.Click();
            }
            catch (Exception ex) when (
                ex is ElementClickInterceptedException ||
                ex is ElementNotInteractableException)
            {
                JsClick(element);
            }
        }

        private void FillBy(By by, string value)
        {
            var element = FindVisible(by);
            ScrollTo(element);
            TryClick(element);
            element.Clear();
            element.SendKeys(value);
            Console.WriteLine($"Champ rempli : {by} = {value}");
        }

        /// <summary>Coche un bouton radio (le vrai input peut être masqué par le CSS, on clique le label).</summary>
        private void ClickRadio(string radioId)
        {
            var radio = Waiter.Until(d => d.FindElements(By.Id(radioId)).FirstOrDefault())
                ?? throw new Exception("Radio introuvable : " + radioId);

            ScrollTo(radio);

            var label = Drv.FindElements(By.CssSelector($"label[for='{radioId}']"))
                           .FirstOrDefault(l => l.Displayed);

            if (label != null) TryClick(label);
            else JsClick(radio);

            if (!radio.Selected)
                JsClick(radio);

            Console.WriteLine("Radio coché : " + radioId);
        }

        /// <summary>
        /// Liste "Selectize" (Drupal) : le vrai select est caché, on passe par le
        /// composant visuel, puis par l'API JS selectize en secours.
        /// </summary>
        private void SelectizeSelect(string selectId, string text)
        {
            string wanted = Normalize(text);
            string dropdownCss = $"#{selectId} + .selectize-control .selectize-dropdown .option";

            // Le composant peut être masqué tant que le profil n'est pas choisi
            var control = FindVisible(
                By.CssSelector($"#{selectId} + .selectize-control .selectize-input"));

            ScrollTo(control);
            TryClick(control);
            Thread.Sleep(300);

            // Saisie pour filtrer
            var search = Drv.FindElements(By.Id(selectId + "-selectized")).FirstOrDefault();
            if (search != null)
            {
                try { search.SendKeys(text); } catch { }
            }

            Thread.Sleep(600);

            var options = Drv.FindElements(By.CssSelector(dropdownCss))
                             .Where(o => o.Displayed).ToList();

            var choice =
                options.FirstOrDefault(o => Normalize(o.Text) == wanted) ??
                options.FirstOrDefault(o => Normalize(o.Text).Contains(wanted));

            if (choice != null)
            {
                TryClick(choice);
                Thread.Sleep(400);
                Console.WriteLine($"Selectize {selectId} : {choice.Text}");
                return;
            }

            // Secours : API JS de Selectize
            var result = ((IJavaScriptExecutor)Drv).ExecuteScript(@"
                var el = document.getElementById(arguments[0]);
                if (!el || !el.selectize) return 'NO_SELECTIZE';
                var s = el.selectize, wanted = arguments[1], found = null, all = [];
                function norm(t){ return (t||'').normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase().trim(); }
                for (var k in s.options) {
                    var txt = norm(s.options[k].text);
                    all.push(s.options[k].text);
                    if (txt === wanted) { found = k; break; }
                    if (found === null && txt.indexOf(wanted) >= 0) found = k;
                }
                if (found === null) return 'NOT_FOUND:' + all.join(' | ');
                s.setValue(found);
                return 'OK';
            ", selectId, wanted)?.ToString();

            if (result != "OK")
                throw new Exception(
                    $"Impossible de sélectionner '{text}' dans {selectId}. Détail : {result}");

            Console.WriteLine($"Selectize {selectId} : {text} (via API JS)");
        }

        /// <summary>
        /// Autocomplete Drupal (jQuery UI). Si la valeur du jeu de données ne donne
        /// aucune suggestion (ex. "Francaise" au lieu de "France"), on raccourcit
        /// progressivement le texte ("Franc" -> "France").
        /// </summary>
        private void FillAutocomplete(string inputId, string value)
        {
            var input = FindVisible(By.Id(inputId));
            ScrollTo(input);
            TryClick(input);

            string attempt = value;

            while (true)
            {
                input.Clear();
                input.SendKeys(attempt);
                Thread.Sleep(1500);

                var suggestions = Drv.FindElements(
                        By.CssSelector("ul.ui-autocomplete li.ui-menu-item"))
                    .Where(s => s.Displayed).ToList();

                if (suggestions.Count > 0)
                {
                    var choice =
                        suggestions.FirstOrDefault(
                            s => Normalize(s.Text) == Normalize(value)) ??
                        suggestions.First();

                    Console.WriteLine($"Nationalité choisie : {choice.Text} (saisie : {attempt})");
                    TryClick(choice);
                    Thread.Sleep(600);
                    return;
                }

                if (attempt.Length <= 3)
                    break;

                attempt = attempt.Substring(0, Math.Max(3, attempt.Length - 2));
            }

            throw new Exception($"Aucune suggestion trouvée pour la nationalité '{value}'.");
        }

        /// <summary>Minuscules + suppression des accents pour comparer les textes.</summary>
        private static string Normalize(string s)
        {
            string decomposed = (s ?? "").Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().ToLowerInvariant();
        }
    }
}